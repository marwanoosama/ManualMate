using AutoMapper;
using ManualMate.Application.DTOs;
using ManualMate.Domain.Models;

namespace ManualMate.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<GetItemDto, Item>().ReverseMap();
            CreateMap<CreateItemDto, Item>().ReverseMap();
            CreateMap<UpdateItemDto, Item>(); // was missing: PUT /api/item/{id} threw "Missing type map configuration"
            CreateMap<Item, ItemResponseDto>().ReverseMap();
        }
    }
}