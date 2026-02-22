using Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Text;

namespace Core.Model
{
    public class Category : IEntity
    {
        public int CategoriesId { get; private set; }        

        public string CategoryName { get; protected set; }

        public int CategoryTypeId { get; protected set; }

        public int? ParentCategoryId { get; protected set; }
        public int AccountId { get; protected set; }

        public Account? Account { get; protected set; }
        public Category? ParentCategory { get; protected set; }
        public CategoryType? Type { get; protected set; }

        private readonly List<Category> _subCategories = new();
        public virtual IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

        public Category([NotNull] string categoryName, int categoryTypeId, int accountId, int? parentCategoryId = null)
        {
            ValidateCategoryParameters(categoryName);
            ValidateAccountId(accountId);

            if (categoryTypeId <= 0)
            {
                throw new ArgumentException("O ID do Tipo de Categoria é obrigatório.", nameof(categoryTypeId));
            }

            CategoriesId = default;            

            CategoryName = categoryName;
            CategoryTypeId = categoryTypeId;
            ParentCategoryId = parentCategoryId;
            AccountId = accountId;
        }

        private Category(int id, int? parentCategoryId, string categoryName,
            int categoryTypeId, int accountId)
        {
            CategoriesId = id;           

            CategoryName = categoryName;
            CategoryTypeId = categoryTypeId;
            ParentCategoryId = parentCategoryId;
            AccountId = accountId;
        }

        public static Category Reconstitute(int id, int? parentCategoryId, string categoryName,
            int categoryTypeId, int accountId)
        {
            return new Category(id, parentCategoryId, categoryName, categoryTypeId, accountId);
        }

        public void UpdateDetails(string newCategoryName, int newCategoryTypeId, int? newParentCategoryId)
        {

            ValidateCategoryParameters(newCategoryName);

            if (CategoryName != newCategoryName)
            {
                CategoryName = newCategoryName;
            }

            if (CategoryTypeId != newCategoryTypeId)
            {
                CategoryTypeId = newCategoryTypeId;
            }
            if (ParentCategoryId != newParentCategoryId)
            {
                ParentCategoryId = newParentCategoryId;
            }
        }     

        public void ChangeParent(int? newParentCategoryId)
        {
            if (newParentCategoryId.HasValue && newParentCategoryId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newParentCategoryId), "O ID deve ser nulo ou positivo.");
            }
            if (ParentCategoryId != newParentCategoryId)
            {
                ParentCategoryId = newParentCategoryId;
            }

            ParentCategory = null;
        }

        public void SetAccount(Account account)
        {
            if (AccountId != account.AccountId)
            {
                throw new InvalidOperationException("Erro de mapeamento: O ID da Account não coincide com o ID injetado.");
            }
            Account = account;
        }

        public void SetSubCategories(IEnumerable<Category> subCategories)
        {
            _subCategories.Clear();
            _subCategories.AddRange(subCategories);
        }

        public void SetParentCategory(Category parentCategory)
        {
            ParentCategory = parentCategory;

            if (ParentCategoryId != parentCategory.CategoriesId)
            {
                throw new InvalidOperationException("Erro de mapeamento: O ID da categoria não coincide com o ID injetado");
            }
        }

        private void ValidateAccountId(int accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("O ID da Account é obrigatório.", nameof(accountId));
            }
        }

        private void ValidateCategoryParameters([NotNull] string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentException("O nome da categoria é obrigatório.", nameof(categoryName));
            }

            if (categoryName.Length > 255)
            {
                throw new ArgumentException("O nome da categoria não pode exceder 255 caracteres.", nameof(categoryName));
            }
        }

        public int GetId() => CategoriesId;

        public void SetId(int id)
        {
            if (CategoriesId != 0)
            {
                throw new InvalidOperationException("Não é permitido alterar o ID de uma Entidade que já possui um ID.");
            }
            CategoriesId = id;
        }

        public bool GetIsActive() => true;
    }
}
