
using MensajeService.DTOs.UserBlocks;
using MensajeService.Models;
using MensajeService.Repositories;

namespace MensajeService.Services;

public class UserBlockService
{
    private readonly UserBlockRepository _repository;

    public UserBlockService(UserBlockRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserBlockResponseDto> BlockUserAsync(Guid blockerId, Guid blockedId)
    {
        ValidateUserIds(blockerId, blockedId);

        if (blockerId == blockedId)
            throw new ArgumentException("You cannot block yourself.");

        var existingBlock = await _repository.GetByUsersAsync(blockerId, blockedId);
        if (existingBlock is not null)
            throw new InvalidOperationException("User is already blocked.");

        var block = new UserBlock
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(block);
        await _repository.SaveChangesAsync();

        return MapToDto(block);
    }

    public async Task<List<UserBlockResponseDto>> GetBlockedUsersAsync(Guid blockerId)
    {
        ValidateBlockerId(blockerId);

        var blocks = await _repository.GetBlockedUsersAsync(blockerId);

        return blocks.Select(MapToDto).ToList();
    }

    public async Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId)
    {
        ValidateUserIds(blockerId, blockedId);

        return await _repository.HasBlockedAsync(blockerId, blockedId);
    }

    public async Task UnblockUserAsync(Guid blockerId, Guid blockedId)
    {
        ValidateUserIds(blockerId, blockedId);

        var block = await _repository.GetByUsersAsync(blockerId, blockedId);
        if (block is null)
            throw new KeyNotFoundException("Block not found.");

        _repository.Delete(block);

        await _repository.SaveChangesAsync();
    }

    private static void ValidateUserIds(Guid blockerId, Guid blockedId)
    {
        ValidateBlockerId(blockerId);

        if (blockedId == Guid.Empty)
            throw new ArgumentException("Invalid blocked user ID.");
    }

    private static void ValidateBlockerId(Guid blockerId)
    {
        if (blockerId == Guid.Empty)
            throw new ArgumentException("Invalid blocker user ID.");
    }

    private static UserBlockResponseDto MapToDto(UserBlock block)
    {
        return new UserBlockResponseDto
        {
            Id = block.Id,
            BlockedId = block.BlockedId,
            CreatedAt = block.CreatedAt
        };
    }
}
