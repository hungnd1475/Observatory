using Observatory.Core.Models;
using AM = AutoMapper;
using MG = Microsoft.Graph;

namespace Observatory.Providers.Exchange.Mappings
{
    public class FolderProfile : AM.Profile
    {
        public FolderProfile()
        {
            CreateMap<MG.MailFolder, MailFolder>()
                .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dst => dst.ParentId, opt => opt.MapFrom(src => src.ParentFolderId))
                .ForMember(dst => dst.IsFavorite, opt => opt.MapFrom(src => false))
                .ForMember(dst => dst.Type, opt => opt.MapFrom(src => FolderType.Normal))
                .ForAllMembers(opt => opt.Condition((src, dst, prop) => prop != null));

            CreateMap<MailFolder, MG.MailFolder>()
                .ForMember(dst => dst.DisplayName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dst => dst.ParentFolderId, opt => opt.MapFrom(src => src.ParentId))
                .ForAllMembers(opt => opt.Condition((src, dst, prop) => prop != null));
        }
    }
}
