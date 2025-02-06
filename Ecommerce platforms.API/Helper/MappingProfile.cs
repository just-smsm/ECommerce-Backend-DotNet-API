using AutoMapper;
using Ecommerce_platforms.API.ModelsDTO;
using Ecommerce_platforms.Core.Models;

namespace Ecommerce_platforms.API.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapping for Product to ProductDTO
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.Name))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PictureUrl, opt => opt.MapFrom(src =>
                    src.PictureUrl != null ? $"https://localhost:7280{src.PictureUrl}" : null))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.Count > 0));

            // Mapping for ProductDTO to Product
            CreateMap<ProductDTO, Product>()
                .ForMember(dest => dest.BrandId, opt => opt.Ignore())  // Ensure BrandId is managed elsewhere
                .ForMember(dest => dest.PictureUrl, opt => opt.MapFrom(src => src.PictureUrl));

           
            CreateMap<BrandDTO, Brand>()
                .ForMember(dest => dest.PictureUrl, opt => opt.Ignore()); // Ignore it, since we manually set PictureUrl

            // Mapping for BrandDTO to Brand
            CreateMap<BrandDTO, Brand>();

            // Mapping for CartItem and CartItemResponse
            CreateMap<CartItem, CartItemResponse>()
                .ForMember(dest => dest.PictureUrl, opt => opt.MapFrom(src => src.PictureUrl))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ProductId));
            // Mapping for Product and CreateNewProductDto
            CreateMap<CreateNewProductDTO, Product>();

        }
    }
}
