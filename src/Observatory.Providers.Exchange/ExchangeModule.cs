using Autofac;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Observatory.Core.Models;
using Observatory.Core.Services;
using Observatory.Providers.Exchange.Persistence;
using Observatory.Providers.Exchange.Services;
using System;
using AM = AutoMapper;
using MG = Microsoft.Graph;

namespace Observatory.Providers.Exchange
{
    public class ExchangeModule : Module
    {
        public static AM.MapperConfiguration MapperConfiguration { get; }

        static ExchangeModule()
        {
            MapperConfiguration = new AM.MapperConfiguration(cfg =>
            {
                cfg.AddExpressionMapping();
                cfg.AddProfile<MessageProfile>();
            });
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ExchangeAuthenticationService>()
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<ExchangeProfileDataStore>()
                .InstancePerDependency();
            builder.RegisterType<ExchangeProfileProvider>()
                .As<IProfileProvider>()
                .Keyed<IProfileProvider>(ExchangeProfileProvider.PROVIDER_ID)
                .SingleInstance();
        }
    }

    public class MessageProfile : AM.Profile
    {
        class BodyResolver : IValueResolver<Message, MG.Message, MG.ItemBody>
        {
            public MG.ItemBody Resolve(Message source, MG.Message destination, MG.ItemBody destMember, ResolutionContext context)
            {
                if (source.Body != null || source.BodyType != null)
                {
                    if (destMember == null) destMember = new MG.ItemBody();
                    destMember.Content = source.Body;
                    destMember.ContentType = context.Mapper.Map<MG.BodyType?>(source.BodyType);
                }
                return destMember;
            }
        }

        class FlagResolver : IValueResolver<Message, MG.Message, MG.FollowupFlag>
        {
            public MG.FollowupFlag Resolve(Message source, MG.Message destination, MG.FollowupFlag destMember, ResolutionContext context)
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
                .ForMember(dst => dst.Body, opt => opt.MapFrom(src => src.Body.Content))
                .ForMember(dst => dst.BodyType, opt => opt.MapFrom(src => src.Body.ContentType))
                .ForMember(dst => dst.FolderId, opt => opt.MapFrom(src => src.ParentFolderId))
                .ForMember(dst => dst.ThreadId, opt => opt.MapFrom(src => src.ConversationId))
                .ForMember(dst => dst.IsFlagged, opt => opt.MapFrom(src => src.Flag.FlagStatus))
                .ForAllMembers(opt => opt.Condition((src, dst, prop) => prop != null));

            CreateMap<Message, MG.Message>()
                .ForMember(dst => dst.Body, opt => opt.MapFrom(new BodyResolver()))
                .ForMember(dst => dst.ParentFolderId, opt => opt.MapFrom(dst => dst.FolderId))
                .ForMember(dst => dst.ConversationId, opt => opt.MapFrom(dst => dst.ThreadId))
                .ForMember(dst => dst.Flag, opt => opt.Condition(src => src.IsFlagged != null))
                .ForMember(dst => dst.Flag, opt => opt.MapFrom(new FlagResolver()))
                .ForAllMembers(opt =>
                {
                    opt.Condition((src, dst, prop) => prop != null);
                    opt.AllowNull();
                });
        }
    }
}
