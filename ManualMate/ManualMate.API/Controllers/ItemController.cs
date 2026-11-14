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
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetItem(int id)
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, UpdateItemDto dto)
        {
            var result = await itemService.EditItemAsync(id, dto);
            return this.GetResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var result = await itemService.DeleteAsync(id);
            return this.GetResponse(result);
        } 
        #endregion

        [HttpPost("upload-manual/{id:int}")]
        public async Task<IActionResult> UploadManual(int id, IFormFile file)
        {
            var result = await fileUploadService.UploadItemFileAsync(id, file);
            return this.GetResponse(result);
        }
    }
}