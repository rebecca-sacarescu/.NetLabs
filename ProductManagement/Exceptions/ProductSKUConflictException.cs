namespace ProductManagement.Exceptions;

public class ProductSKUConflictException : BaseException
{
    public ProductSKUConflictException(string sku) 
        : base($"A product with SKU '{sku}' already exists.", 409, "PRODUCT_SKU_CONFLICT")
    {
    }
}