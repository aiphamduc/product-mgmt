namespace ProductManagement.Core.Entities;

public static class ProductDomainRules
{
    public static IReadOnlyCollection<string> ValidateAttributes(Category category, IReadOnlyCollection<AttributeInput> inputs)
    {
        var errors = new List<string>();
        var definitions = category.AttributeDefinitions.ToDictionary(definition => definition.Id);
        var duplicateIds = inputs.GroupBy(input => input.DefinitionId).Where(group => group.Count() > 1).Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add($"Attribute '{duplicateId}' was supplied more than once.");
        }

        foreach (var input in inputs)
        {
            if (!definitions.TryGetValue(input.DefinitionId, out var definition))
            {
                errors.Add($"Attribute '{input.DefinitionId}' is not defined for category '{category.Id}'.");
                continue;
            }

            if (!CanParseAttribute(definition.ValueType, input.Value))
            {
                errors.Add($"Attribute '{definition.Name}' has an invalid {definition.ValueType} value.");
            }
        }

        var suppliedIds = inputs.Select(input => input.DefinitionId).ToHashSet();
        foreach (var required in category.AttributeDefinitions.Where(definition => definition.IsRequired && !suppliedIds.Contains(definition.Id)))
        {
            errors.Add($"Required attribute '{required.Name}' is missing.");
        }

        return errors;
    }

    public static ProductAttributeValue ToAttributeValue(AttributeDefinitionSnapshot definition, Guid productId, string value)
    {
        var result = new ProductAttributeValue { ProductId = productId, AttributeDefinitionId = definition.Id };
        switch (definition.ValueType)
        {
            case AttributeValueType.Integer:
                result.IntegerValue = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            case AttributeValueType.Decimal:
                result.DecimalValue = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            case AttributeValueType.Boolean:
                result.BooleanValue = bool.Parse(value);
                break;
            default:
                result.TextValue = value.Trim();
                break;
        }

        return result;
    }

    private static bool CanParseAttribute(AttributeValueType type, string value) => type switch
    {
        AttributeValueType.Text => !string.IsNullOrWhiteSpace(value),
        AttributeValueType.Integer => int.TryParse(value, out _),
        AttributeValueType.Decimal => decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
        AttributeValueType.Boolean => bool.TryParse(value, out _),
        _ => false
    };

    public static bool CanTransition(ProductStatus current, ProductStatus requested) =>
        (current, requested) switch
        {
            (ProductStatus.Draft, ProductStatus.Published) => true,
            (ProductStatus.Published, ProductStatus.Draft) => true,
            (ProductStatus.Draft, ProductStatus.Archived) => true,
            (ProductStatus.Published, ProductStatus.Archived) => true,
            _ => false
        };

    public static IReadOnlyCollection<string> ValidateForPublish(Product product)
    {
        var errors = new List<string>();

        if (product.Category is null || !product.Category.IsActive)
        {
            errors.Add("An active category is required.");
        }

        if (product.Variants.Count == 0)
        {
            errors.Add("At least one variant is required.");
        }
        else if (!product.Variants.Any(variant => variant.StockOnHand > 0 && variant.Price >= 0))
        {
            errors.Add("At least one sellable variant is required.");
        }

        var requiredDefinitions = product.Category?.AttributeDefinitions
            .Where(definition => definition.IsRequired)
            .Select(definition => definition.Id)
            .ToHashSet() ?? new HashSet<Guid>();
        var providedDefinitions = product.AttributeValues.Select(value => value.AttributeDefinitionId).ToHashSet();
        if (requiredDefinitions.Except(providedDefinitions).Any())
        {
            errors.Add("All required category attributes must be provided.");
        }

        return errors;
    }

    public static IReadOnlyCollection<string> ValidateVariant(decimal price, string currency, int stockOnHand)
    {
        var errors = new List<string>();
        if (price < 0) errors.Add("Price cannot be negative.");
        if (!string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)) errors.Add("Currency must be USD.");
        if (stockOnHand < 0) errors.Add("Stock cannot be negative.");
        return errors;
    }

    public static IReadOnlyCollection<string> ValidateStockAdjustment(int quantity)
    {
        return quantity == 0
            ? new[] { "Stock adjustment quantity cannot be zero." }
            : Array.Empty<string>();
    }
}

public sealed record AttributeInput(Guid DefinitionId, string Value);

public sealed record AttributeDefinitionSnapshot(Guid Id, AttributeValueType ValueType);
