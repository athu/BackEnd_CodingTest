using System.Collections.Generic;
using System.Linq;

namespace WebApi.Helpers
{
    public class Utility
    {
        public static List<T> Pagination<T>(List<T> list, int page, int limit)
        {
            if (limit <= 0)
            {
                limit = 10;
            }

            if (page > 0)
            {
                list = list.Skip((page - 1) * limit).Take(limit).ToList();
            }

            return list;
        }
    }
}
