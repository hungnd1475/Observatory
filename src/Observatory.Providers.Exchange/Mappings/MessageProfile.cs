using Observatory.Core.Models;
using Observatory.Core.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using AM = AutoMapper;
using MG = Microsoft.Graph;

namespace Observatory.Providers.Exchange.Mappings
{
    public class MessageProfile : AM.Profile
    {
        //class BodyResolver : AM.IValueResolver<Message, MG.Message, MG.ItemBody>
        //{
        //    public MG.ItemBody Resolve(Message source, MG.Message destination, MG.ItemBody destMember, AM.ResolutionContext context)
        //    {
        //        if (source.Body != null || source.BodyType != null)
        //        {
        //            if (destMember == null) destMember = new MG.ItemBody();
        //            destMember.Content = source.Body;
        //            destMember.ContentType = context.Mapper.Map<MG.BodyType?>(source.BodyType);
        //        }
        //        return destMember;
        //    }
        //}

        class FlagResolver : AM.IValueResolver<UpdatableMessage, MG.Message, MG.FollowupFlag>
        {
            public MG.FollowupFlag Resolve(UpdatableMessage source, MG.Message destination, MG.FollowupFlag destMember, AM.ResolutionContext context)
            {
                if (source.IsFlagged != null)
                {
                    if (destMember == null) destMember = new MG.FollowupFlag();
                    destMember.FlagStatus = context.Mapper.Map<MG.FollowupFlagStatus?>(source.IsFlagged);
                }
                return destMember;
            }
        }

        public MessageProfile()
        {
            CreateMap<MG.Recipient, Recipient>()
                .ForMember(dst => dst.DisplayName, opt => opt.MapFrom(src => src.EmailAddress.Name))
                .ForMember(dst => dst.EmailAddress, opt => opt.MapFrom(src => src.EmailAddress.Address))
                .ReverseMap()
                .ForPath(src => src.EmailAddress.Name, opt => opt.MapFrom(dst => dst.DisplayName))
                .ForPath(src => src.EmailAddress.Address, opt => opt.MapFrom(dst => dst.EmailAddress));

            CreateMap<MG.Importance, Importance>()
                .ReverseMap();

            CreateMap<MG.BodyType, ContentType>()
                .ReverseMap();

            CreateMap<MG.FollowupFlagStatus, bool>()
                .ConvertUsing(src => src == MG.FollowupFlagStatus.Flagged);

            CreateMap<bool, MG.FollowupFlagStatus>()
                .ConvertUsing(dst => dst ? MG.FollowupFlagStatus.Flagged : MG.FollowupFlagStatus.NotFlagged);

            CreateMap<MG.Message, Message>()
                .ForMember(dst => dst.Body, opt =>
                {
                    opt.PreCondition(src => src.Body != null);
                    opt.MapFrom(src => src.Body.Content);
                })
                .ForMember(dst => dst.BodyPreview, opt => opt.PreCondition(src => src.BodyPreview != null))
                .ForMember(dst => dst.BodyType, opt =>
                {
                    opt.PreCondition(src => src.Body != null);
                    opt.MapFrom(src => src.Body.ContentType);
                })
                .ForMember(dst => dst.CcRecipients, opt => opt.PreCondition(src => src.CcRecipients != null))
                .ForMember(dst => dst.FolderId, opt =>
                {
                    opt.PreCondition(src => src.ParentFolderId != null);
                    opt.MapFrom(src => src.ParentFolderId);
                })
                .ForMember(dst => dst.HasAttachments, opt => opt.PreCondition(src => src.HasAttachments.HasValue))
                .ForMember(dst => dst.Importance, opt => opt.PreCondition(src => src.Importance.HasValue))
                .ForMember(dst => dst.IsDraft, opt => opt.PreCondition(src => src.IsDraft.HasValue))
                .ForMember(dst => dst.IsFlagged, opt =>
                {
                    opt.PreCondition(src => src.Flag != null && src.Flag.FlagStatus.HasValue);
                    opt.MapFrom(src => src.Flag.FlagStatus);
                })
                .ForMember(dst => dst.IsRead, opt => opt.PreCondition(src => src.IsRead.HasValue))
                .ForMember(dst => dst.ReceivedDateTime, opt => opt.PreCondition(src => src.ReceivedDateTime.HasValue))
                .ForMember(dst => dst.Sender, opt => opt.PreCondition(src => src.Sender != null))
                .ForMember(dst => dst.Subject, opt => opt.PreCondition(src => src.Subject != null))
                .ForMember(dst => dst.ThreadId, opt =>
                {
                    opt.PreCondition(src => src.ConversationId != null);
                    opt.MapFrom(src => src.ConversationId);
                })
                .ForMember(dst => dst.ThreadPosition, opt => opt.Ignore())
                .ForMember(dst => dst.ToRecipients, opt => opt.PreCondition(src => src.ToRecipients != null));

            CreateMap<UpdatableMessage, MG.Message>()
                .ForMember(dst => dst.IsRead, opt => opt.Condition((src, dst, prop) => prop.HasValue))
                .ForMember(dst => dst.Flag, opt => opt.MapFrom(new FlagResolver()));
        }
    }
}
