using Observatory.Core.Models;
using Observatory.Core.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Mappings
{
    public class MessageProfile : AutoMapper.Profile
    {
        public MessageProfile()
        {
            CreateMap<UpdatableMessage, Message>()
                .ForMember(dst => dst.IsRead, opt => opt.Condition(src => src.IsRead.HasValue))
                .ForMember(dst => dst.IsFlagged, opt => opt.Condition(src => src.IsFlagged.HasValue));
        }
    }
}
