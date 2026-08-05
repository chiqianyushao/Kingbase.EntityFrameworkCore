using AbpBookStore.Authors;
using AbpBookStore.Books;
using AutoMapper;

namespace AbpBookStore;

public class BookStoreApplicationAutoMapperProfile : Profile
{
    public BookStoreApplicationAutoMapperProfile()
    {
        CreateMap<Book, BookDto>()
            .ForMember(
                dest => dest.Authors,
                opt => opt.MapFrom(src => src.BookAuthors.Select(ba => ba.Author)));

        CreateMap<CreateUpdateBookDto, Book>()
            .ForMember(dest => dest.BookAuthors, opt => opt.Ignore());

        CreateMap<Author, AuthorDto>();
        CreateMap<CreateUpdateAuthorDto, Author>();
    }
}
