using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models
{
    public class AsdsResultModel
    {
        public string predictString { get; set; }
        public string errorMsg { get; set; }
        public bool result { get; set; }
        public int answer { get; set; }
        public float probability { get; set; }
    }

    public class AsdsRecordModel
    {
        public long id { get; set; }
        public string str { get; set; }
        public bool answer { get; set; }
        public float probability { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime regDate { get; set; }
    }
}