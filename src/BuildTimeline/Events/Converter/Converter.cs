using System;
using System.Collections.Generic;

namespace BuildTimeline
{
    public class Converter<InputType, OutputType>
    {
        private Dictionary<Type, Func<InputType, OutputType>> m_converters;

        public Converter()
        {
            m_converters = new Dictionary<Type, Func<InputType, OutputType>>();
        }

        public OutputType Convert(InputType e)
        {
            Func<InputType, OutputType> converter = null;
            bool found = m_converters.TryGetValue(e.GetType(), out converter);

            if(found)
            {
                return converter(e);
            }
            
            return default(OutputType);
        }

        public void Register<T>(Func<InputType, OutputType> converter) where T : InputType
        {
            m_converters.Add(typeof(T), converter);
        }
    }
}
