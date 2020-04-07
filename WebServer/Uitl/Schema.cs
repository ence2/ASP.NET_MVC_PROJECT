using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public class predict_record
    {
        [AutoIncrement]
        [PrimaryKey]
        public long DBID { get; set; }
        public float prob { get; set; }
        public int answer { get; set; }
        public string content { get; set; }
        public DateTime regDate { get; set; }
    }
}