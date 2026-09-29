using ProductManagement.Core.Entities;
using Xunit;

namespace ProductManagement.UnitTests;

public sealed class ProductDomainRulesTests
{
    [Fact]
    public void ArchivedProductCannotTransition()
    {
        Assert.False(ProductDomainRules.CanTransition(ProductStatus.Archived, ProductStatus.Draft));
    }

    [Fact]
    public void DraftWithActiveCategoryAndSellableVariantCanPublish()
    {
        var category = new Category { Id = Guid.NewGuid(), IsActive = true };
        var product = new Product
        {
            Category = category,
            Variants = { new ProductVariant { Price = 10, Currency = "USD", StockOnHand = 1 } }
        };

        Assert.Empty(ProductDomainRules.ValidateForPublish(product));
    }

    [Fact]
    public void ZeroStockAdjustmentIsRejected()
    {
        Assert.NotEmpty(ProductDomainRules.ValidateStockAdjustment(0));
    }
}
