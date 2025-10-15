using Catalog.Application.DTOs;

namespace Catalog.Application.Commands;

// Create Product
public record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    Guid CategoryId,
    string Brand,
    decimal Price,
    string Currency = "USD"
) : ICommand<Guid>;

public class CreateProductCommandHandler(
    ICatalogProductRepository repository,
    ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Check if SKU already exists
            var existingProduct = await repository.GetBySkuAsync(command.Sku, cancellationToken);
            if (existingProduct != null)
            {
                return Result.Failure<Guid>(Error.Conflict($"Product with SKU '{command.Sku}' already exists"));
            }

            var product = CatalogProduct.Create(
                command.Name,
                command.Description,
                command.Sku,
                command.CategoryId,
                command.Brand,
                command.Price,
                command.Currency
            );

            await repository.AddAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created product {ProductId} with SKU {Sku}", product.Id, command.Sku);

            return Result<Guid>.Success(product.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product with SKU {Sku}", command.Sku);
            return Result.Failure<Guid>(Error.Unexpected("An error occurred while creating the product"));
        }
    }
}

// Update Product
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    string Brand
) : ICommand<bool>;

public class UpdateProductCommandHandler(
    ICatalogProductRepository repository,
    ILogger<UpdateProductCommandHandler> logger)
    : ICommandHandler<UpdateProductCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.UpdateDetails(command.Name, command.Description, command.Brand);

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated product {ProductId}", command.ProductId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while updating the product"));
        }
    }
}

// Update Price
public record UpdatePriceCommand(
    Guid ProductId,
    decimal Price,
    decimal? CompareAtPrice
) : ICommand<bool>;

public class UpdatePriceCommandHandler(
    ICatalogProductRepository repository,
    ILogger<UpdatePriceCommandHandler> logger)
    : ICommandHandler<UpdatePriceCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdatePriceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.UpdatePrice(command.Price, command.CompareAtPrice);

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated price for product {ProductId} to {Price}", command.ProductId, command.Price);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating price for product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while updating the price"));
        }
    }
}

// Publish Product
public record PublishProductCommand(Guid ProductId) : ICommand<bool>;

public class PublishProductCommandHandler(
    ICatalogProductRepository repository,
    ILogger<PublishProductCommandHandler> logger)
    : ICommandHandler<PublishProductCommand, bool>
{
    public async Task<Result<bool>> Handle(PublishProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.Publish();

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Published product {ProductId}", command.ProductId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while publishing the product"));
        }
    }
}

// Unpublish Product
public record UnpublishProductCommand(Guid ProductId) : ICommand<bool>;

public class UnpublishProductCommandHandler(
    ICatalogProductRepository repository,
    ILogger<UnpublishProductCommandHandler> logger)
    : ICommandHandler<UnpublishProductCommand, bool>
{
    public async Task<Result<bool>> Handle(UnpublishProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.Unpublish();

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Unpublished product {ProductId}", command.ProductId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unpublishing product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while unpublishing the product"));
        }
    }
}

// Add Image to Product
public record AddImageCommand(
    Guid ProductId,
    string Url,
    string AltText,
    bool IsPrimary
) : ICommand<bool>;

public class AddImageCommandHandler(
    ICatalogProductRepository repository,
    ILogger<AddImageCommandHandler> logger)
    : ICommandHandler<AddImageCommand, bool>
{
    public async Task<Result<bool>> Handle(AddImageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.AddImage(command.Url, command.AltText, 0, command.IsPrimary);

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added image to product {ProductId}", command.ProductId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding image to product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while adding the image"));
        }
    }
}

// Add Attribute to Product
public record AddAttributeCommand(
    Guid ProductId,
    string Key,
    string Value,
    string? DisplayName
) : ICommand<bool>;

public class AddAttributeCommandHandler(
    ICatalogProductRepository repository,
    ILogger<AddAttributeCommandHandler> logger)
    : ICommandHandler<AddAttributeCommand, bool>
{
    public async Task<Result<bool>> Handle(AddAttributeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.AddAttribute(command.Key, command.Value, command.DisplayName);

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added attribute to product {ProductId}", command.ProductId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding attribute to product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while adding the attribute"));
        }
    }
}

// Set Featured Status
public record SetFeaturedCommand(Guid ProductId, bool IsFeatured) : ICommand<bool>;

public class SetFeaturedCommandHandler(
    ICatalogProductRepository repository,
    ILogger<SetFeaturedCommandHandler> logger)
    : ICommandHandler<SetFeaturedCommand, bool>
{
    public async Task<Result<bool>> Handle(SetFeaturedCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Product with ID {command.ProductId} not found"));
            }

            product.SetAsFeatured(command.IsFeatured);

            await repository.UpdateAsync(product, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Set featured status for product {ProductId} to {IsFeatured}", 
                command.ProductId, command.IsFeatured);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting featured status for product {ProductId}", command.ProductId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while updating featured status"));
        }
    }
}
