using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace QuartzService
{
    public class QuartzConfig : Dictionary<string, string>
    {
        public QuartzConfig UpdateConnectionString(string connectionString)
        {
            this["quartz.dataSource.quartzDS.connectionString"] = connectionString;
            return this;
        }

        public NameValueCollection ToNameValueCollection()
        {
            return this.Aggregate(new NameValueCollection(), (seed, current) =>
            {
                seed.Add(current.Key, current.Value);
                return seed;
            });
        }
    }
}