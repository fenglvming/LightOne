﻿using Common.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business {
    public class CategoryRepo {
        public static readonly MongoCollection<Category> Collection;

        static CategoryRepo() {
            Collection = DatabaseFactory.CreateMongoDatabase().GetCollection<Category>("categories");
        }

        public IEnumerable<Category> GetAll() {
            return Collection.FindAll();
        }

        //public IEnumerable<Category> GetBySource(string source) {
        //    return Collection.Find(Query<Category>.EQ(c => c.Source, source));
        //}

        public Category Get(ObjectId id) {
            if (id == ObjectId.Empty)
                return null;
            return Collection.FindOne(Query<Category>.EQ(c => c.Id, id));
        }

        public Category GetBySourceAndNumber(string source, string number) {
            return Collection.FindOne(Query.And(Query<Category>.EQ(c => c.Source, source), Query<Category>.EQ(c => c.Number, number)));
        }

        public IEnumerable<Category> GetByIds(ObjectId[] ids) {
            if (ids == null || ids.Length == 0)
                return null;
            return Collection.Find(Query<Category>.In(p => p.Id, ids));
        }

        public IEnumerable<Category> GetAncestorCategories(ObjectId id) {
            var category = Get(id);
            if (category == null)
                yield break;

            var parent = GetBySourceAndNumber(category.Source, category.ParentNumber);
            if (parent != null) {
                foreach (var c in GetAncestorCategories(parent.Id))
                    yield return c;
            }
            yield return category;
        }

        private IEnumerable<Category> GetByLevel(int level) {
            return Collection.Find(Query<Category>.EQ(c => c.Level, level))
                .SetSortOrder(SortBy<Category>.Ascending(c => c.Sort));
        }

        private IEnumerable<Category> GetBySourceAndParentNumber(string source, string parentNumber) {
            return Collection.Find(Query.And(Query<Category>.EQ(c => c.Source, source), Query<Category>.EQ(c => c.ParentNumber, parentNumber)))
                    .SetSortOrder(SortBy<Category>.Ascending(c => c.Sort));
        }

        public IEnumerable<Tuple<Category, IEnumerable<Category>>> GetCategoryTree(ObjectId categoryId) {
            var category = Get(categoryId);
            if (category == null) {
                // 分类不存在，返回顶级分类
                return GetByLevel(1).Select(c => new Tuple<Category, IEnumerable<Category>>(c, null));
            }

            // 获取下级分类
            var subCategories = GetBySourceAndParentNumber(category.Source, category.Number).ToList();
            var brotherCategories = GetBySourceAndParentNumber(category.Source, category.ParentNumber).ToList();
            if (subCategories.Count > 0) {
                // 有下级分类，返回兄弟分类及当前分类的下级分类
                return brotherCategories.Select(bc => new Tuple<Category, IEnumerable<Category>>(bc, bc.Id == categoryId ? subCategories : null));
            }

            // 无下级分类
            var parentCategory = GetBySourceAndNumber(category.Source, category.ParentNumber);
            if (parentCategory != null) {
                // 返回父分类的兄弟分类及当前分类的兄弟分类
                var parentBrotherCategories = GetBySourceAndParentNumber(parentCategory.Source, parentCategory.ParentNumber);
                return parentBrotherCategories.Select(pbc => new Tuple<Category, IEnumerable<Category>>(pbc, pbc.Number == category.ParentNumber ? brotherCategories : null));
            }
            else {
                // 没有父分类也没有子分类，返回当前分类的兄弟分类
                return brotherCategories.Select(bc => new Tuple<Category, IEnumerable<Category>>(bc, null));
            }
        }

        public void Save(Category category) {
            Collection.Save(category);
        }

        //public void DisableById(string categoryId) {
        //    Collection.Update(Query<Category>.EQ(c => c.Id, categoryId), Update<Category>.Set(c => c.Enable, false));
        //}

        public void UpdateProductsUpdateTime(ObjectId id, DateTime productsUpdateTime) {
            Collection.Update(Query<Category>.EQ(c => c.Id, id),
                    Update<Category>.Set(c => c.ProductsUpdateTime, productsUpdateTime));
        }

        public void ResetStableTimes(ObjectId id) {
            Collection.Update(Query<Category>.EQ(c => c.Id, id),
                    Update<Category>.Set(c => c.StableTimes, 0));
        }

        public void IncreaseStableTimes(ObjectId id, int maxStableTimes = 24) {
            Collection.Update(Query.And(
                    Query<Category>.EQ(c => c.Id, id),
                    Query<Category>.LT(c => c.StableTimes, maxStableTimes)),
                    Update<Category>.Inc(c => c.StableTimes, 1));
        }
    }
}
