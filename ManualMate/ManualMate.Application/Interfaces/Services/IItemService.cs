using ManualMate.Application.DTOs;
using ManualMate.Application.Responses;

namespace ManualMate.Application.Interfaces.Services;

public interface IItemService
{
    Task<Result<GetItemDto>> GetItemAsync(int id);
    Task<Result<IEnumerable<GetItemDto>>> GetItemsAsync();
    Task<Result<CreateItemDto>> CreateItemAsync(CreateItemDto dto);
    Task<Result<GetItemDto>> EditItemAsync(int id, UpdateItemDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}