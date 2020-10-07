using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace Observatory.Core.Persistence.Conversion
{
    public class ObjectToJsonConverter<T> : ValueConverter<T, string>
    {
        public ObjectToJsonConverter() 
            : base(v => JsonSerializer.Serialize(v, null), 
                   v => JsonSerializer.Deserialize<T>(v, null))
        { }
    }
}
