using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StinWeb.Models.DataManager;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class РаботыController : Controller
    {
        private readonly StinDbContext _context;
        public РаботыController(StinDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public JsonResult GetChildren(string id, string izdelieId, bool garantia)
        {
            string Производитель = "";
            if (garantia)
            {
                Производитель = (from sc84 in _context.Sc84s
                                 where sc84.Id == izdelieId
                                 select sc84.Sp8842).FirstOrDefault();
            }
            var items = _context.Sc9875s.FromSqlRaw(@"
            WITH cte_org AS (
                SELECT       
                    s.*
        
                FROM       
                    SC9875 s
	            left join SC11498 c on s.ID = c.PARENTEXT
                WHERE s.ISMARK = 0 and s.ISFOLDER = 2 and " + (garantia == false ? "c.ID is null" : "c.sp11496 = '" + Производитель + "'") + @"
                UNION ALL
                SELECT 
                    e.*
                FROM 
                    SC9875 e
                    INNER JOIN cte_org o 
                        ON o.PARENTID = e.ID
            )
            SELECT distinct * FROM cte_org").AsEnumerable()
            .Where(x => x.Parentid == (id == "#" ? Common.ПустоеЗначение : id))
            .Select(x => new
            {
                id = x.Id,
                parent = id,
                text = x.Descr.Trim(),
                children = x.Isfolder == 1,
                icon = x.Isfolder == 2 ? "jstree-file" : ""
            })
            .OrderBy(x => x.text);
            return Json(items);
        }
        private string GetScalarFunctionResult(string ObjId, int Id)
        {
            SqlParameter resultParam = new SqlParameter
            {
                ParameterName = "@resultCost",
                SqlDbType = System.Data.SqlDbType.VarChar,
                Direction = System.Data.ParameterDirection.Output
            };
            _context.Database.ExecuteSqlRaw("select @resultCost = [dbo].[fn_GetPeriodical](@ObjId, @Id);",
            resultParam,
             new SqlParameter("@ObjId", ObjId),
             new SqlParameter("@Id", Id));
            return (string)resultParam.Value;
        }
    }
}
