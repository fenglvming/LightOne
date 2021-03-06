﻿using System;
using System.Linq;
using System.Web.Mvc;
using Business;
using Queen.Models;
using System.Collections.Generic;
using MongoDB.Bson;

namespace Queen.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CategoryRepo _CategoryRepo = new CategoryRepo();

        //[OutputCache(Duration=600)]
        public ActionResult PriceReduced(ObjectId categoryId) {
            var category = _CategoryRepo.Get(categoryId);

            IEnumerable<Product> products = new Product[0];
            var ratioRankingRepo = new RatioRankingRepo();
            var ranking = ratioRankingRepo.GetByCategoryId(categoryId);
            if (ranking != null) {
                products = ranking.ProductIds.Select(id => Product.GetById(id)).Where(p => p != null);
            }
            //var products = Product.GetByPriceReduced(categoryId, 150, 24);

            ViewBag.CategoryName = category != null ? category.Name : string.Empty;
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryUrl = category != null ? category.Url : string.Empty;

            return View(products);
        }

        public ActionResult Details(ObjectId id) {
            var product = Product.GetById(id);
            if (product == null)
                return View("ProductNotExists");

            var categories = _CategoryRepo.GetByIds(product.CategoryIds);
            var form = new ProductDetailsForm(product, categories);

            return View(form);
        }

        public ActionResult PriceHistoryChart(ObjectId id) {
            var product = Product.GetById(id);
            if (product == null)
                return new EmptyResult();

            // 过去30天
            var days = Enumerable.Range(-30, 31).Select(d => DateTime.Today.AddDays(d));

            var chart = new FusionChartsDriver.MSStepLine();
            chart.Chart = new FusionChartsDriver.Chart {
                ShowValues = 0,
                BgColor = "ffffff",
                ShowBorder = 0,
                Animation = 0,
                FormatNumberScale = 0,
            };
            chart.Categories = new[] { 
                new FusionChartsDriver.Categories {
                    CategoryCollection = days.Select(d => new FusionChartsDriver.Category { Label = d.ToString("M-d") }) 
                } 
            };
            chart.Dataset = new[]{
                new FusionChartsDriver.DataSet {
                    Data = days.Select(d=> {
                        var price = product.GetPriceInDay(d);
                        return new FusionChartsDriver.Data {
                            Value = price,
                            ToolText = string.Format("{0}月{1}日价格 {2:C}", d.Month, d.Day, price)
                        };
                    })
                }
            };
            return Content(chart.ToJson());
        }

        public ActionResult Search(string k) {
            if (string.IsNullOrWhiteSpace(k))
                return RedirectToAction("PriceReduced");

            var p = Product.ParseAndGetByUrl(k);
            if (p == null)
                return View("ProductNotExists");

            return RedirectToAction("Details", new { id = p.Id });
        }
    }

}
