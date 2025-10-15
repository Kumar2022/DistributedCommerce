namespace Catalog.Application.Commands;

// Create Category
public record CreateCategoryCommand(
    string Name,
    string Description,
    string? ImageUrl,
    Guid? ParentCategoryId
) : ICommand<Guid>;

public class CreateCategoryCommandHandler(
    ICategoryRepository repository,
    ILogger<CreateCategoryCommandHandler> logger)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var category = Category.Create(
                command.Name,
                command.Description,
                command.ParentCategoryId
            );

            if (!string.IsNullOrEmpty(command.ImageUrl))
            {
                category.SetImage(command.ImageUrl);
            }

            await repository.AddAsync(category, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created category {CategoryId} with name {Name}", category.Id, command.Name);

            return Result<Guid>.Success(category.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category {Name}", command.Name);
            return Result.Failure<Guid>(Error.Unexpected("An error occurred while creating the category"));
        }
    }
}

// Update Category
public record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string Description,
    string? ImageUrl
) : ICommand<bool>;

public class UpdateCategoryCommandHandler(
    ICategoryRepository repository,
    ILogger<UpdateCategoryCommandHandler> logger)
    : ICommandHandler<UpdateCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var category = await repository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Category with ID {command.CategoryId} not found"));
            }

            category.Update(command.Name, command.Description);
            
            if (!string.IsNullOrEmpty(command.ImageUrl))
            {
                category.SetImage(command.ImageUrl);
            }

            await repository.UpdateAsync(category, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated category {CategoryId}", command.CategoryId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", command.CategoryId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while updating the category"));
        }
    }
}

// Deactivate Category
public record DeactivateCategoryCommand(Guid CategoryId) : ICommand<bool>;

public class DeactivateCategoryCommandHandler(
    ICategoryRepository repository,
    ILogger<DeactivateCategoryCommandHandler> logger)
    : ICommandHandler<DeactivateCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(DeactivateCategoryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var category = await repository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Category with ID {command.CategoryId} not found"));
            }

            category.Deactivate();

            await repository.UpdateAsync(category, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Deactivated category {CategoryId}", command.CategoryId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating category {CategoryId}", command.CategoryId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while deactivating the category"));
        }
    }
}

// Activate Category
public record ActivateCategoryCommand(Guid CategoryId) : ICommand<bool>;

public class ActivateCategoryCommandHandler(
    ICategoryRepository repository,
    ILogger<ActivateCategoryCommandHandler> logger)
    : ICommandHandler<ActivateCategoryCommand, bool>
{
    public async Task<Result<bool>> Handle(ActivateCategoryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var category = await repository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<bool>(Error.NotFound($"Category with ID {command.CategoryId} not found"));
            }

            category.Activate();

            await repository.UpdateAsync(category, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Activated category {CategoryId}", command.CategoryId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating category {CategoryId}", command.CategoryId);
            return Result.Failure<bool>(Error.Unexpected("An error occurred while activating the category"));
        }
    }
}
