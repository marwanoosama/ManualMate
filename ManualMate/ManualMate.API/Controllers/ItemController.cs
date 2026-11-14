using ManualMate.API.Controllers;
using ManualMate.Application.DTOs;
using ManualMate.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManualMate.Controllers
{
    [Route("api/item")]
    [ApiController]
    public class ItemController(IItemService itemService, IFileUploadService fileUploadService) : ControllerBase
    {
        #region CRUD
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetItem(Guid id)
        {
            var result = await itemService.GetItemAsync(id);
            return this.GetResponse(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var result = await itemService.GetItemsAsync();
            return this.GetResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem(CreateItemDto dto)
        {
            var result = await itemService.CreateItemAsync(dto);
            return this.GetResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateItem(Guid id, UpdateItemDto dto)
        {
            var result = await itemService.EditItemAsync(id, dto);
            return this.GetResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var result = await itemService.DeleteAsync(id);
            return this.GetResponse(result);
        } 
        #endregion

        // Restored: this upload endpoint was dropped during an earlier refactor,
        // leaving FileUploadService.UploadItemFileAsync as unreachable dead code.
        // It replaces the stored manual for an existing item (kept under wwwroot/Files).
        // NOTE: it only replaces the file - it does not re-trigger PDF processing,
        // so existing embeddings are not regenerated (see project guide).
        [HttpPost("upload-manual/{id:guid}")]
        public async Task<IActionResult> UploadManual(Guid id, IFormFile file)
        {
            var result = await fileUploadService.UploadItemFileAsync(id, file);
            return this.GetResponse(result);
        }
    }
}