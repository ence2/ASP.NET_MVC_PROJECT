using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using WebServer.Models;
using ServiceStack.OrmLite;
using System.Web.Routing;
using System.Web;
using System.Text;
using System.Data;
using System.IO;
using System.Net;

namespace WebServer.Controllers
{
    public class ASDSController : Controller
    {
        // GET: ASDS
        public ActionResult Index()
        {
            return View();
        }

        public class ReqData
        {
            public string targetString;
        }

        public class ResData
        {
            public int answer;
            public float left;
            public float right;
        }

        public ActionResult Predict(AsdsResultModel model)
        {
            if (string.IsNullOrEmpty(model.predictString))
            {
                ModelState.AddModelError("errorMsg", "한 글자 이상 입력해야 합니다.");
                return View("Index");
            }

            if (model.predictString.Length > 196)
            {
                ModelState.AddModelError("errorMsg", "196자를 초과 할 수 없습니다.");
                return View("Index");
            }

            try
            {
                var res = HttpUtility.HttpPostPerform(ServerConfig.Instance.Data.BindDeepLearningModelUrl + "/predict", new ReqData { targetString = model.predictString });
                StreamReader reader = new StreamReader(res.GetResponseStream());
                var resData = NetSerializer.ToObject<ResData>(reader.ReadToEnd());

                using (var userDB = DataContext.OpenUserDB())
                {
                    userDB.Insert<predict_record>(new predict_record { prob = resData.right, content = model.predictString, answer = resData.answer, regDate = DateTime.Now });
                }

                model.result = true;
                model.probability = float.Parse(resData.right.ToString("N2"));
                model.answer = resData.answer;
            }
            catch (Exception e)
            {
                ModelState.AddModelError("errorMsg", "exception : " + e.Message);
                return View("Index");
            }

            return View("Index", model);
        }

        public ActionResult Record_Read([DataSourceRequest]DataSourceRequest request)
        {
            List<AsdsRecordModel> models = new List<AsdsRecordModel>();

            using (var userDB = DataContext.OpenUserDB())
            {
                var rows = userDB.Select<predict_record>().OrderByDescending(w => w.regDate);
                foreach (var r in rows)
                {
                    AsdsRecordModel model = new AsdsRecordModel();
                    model.id = r.DBID;
                    model.str = r.content;
                    model.answer = Convert.ToBoolean(r.answer);
                    model.probability = r.prob;
                    model.regDate = r.regDate;

                    models.Add(model);
                }
            }

            return Json(models.ToDataSourceResult(request));
        }
    }
}