using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlToDb
{
    internal class order
    {
        public int orderId;
        public int no;
        public DateTime reg_date;
        public decimal sum;
        public IEnumerable<product> products;
        public user user;
    }
}