using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace XmlToDb
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Строка подключения к базе в файле App.config
            SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString);

            //Парсинг xml файла
            var ord = from orders in XDocument.Load(Path.Combine(Environment.CurrentDirectory, "XmlDoc.xml")).Descendants("order")
                      select new order
                      {
                          orderId = (int)orders.Element("orderId"),
                          no = (int)orders.Element("no"),
                          reg_date = (DateTime)orders.Element("reg_date"),
                          sum = (decimal)orders.Element("sum"),
                          products = orders.Elements("product").Select(orde => new product
                          {
                              productId = (int)orde.Element("productId"),
                              name = orde.Element("name").Value.ToString(),
                              quantity = (int)orde.Element("quantity"),
                              price = (decimal)orde.Element("price")
                          }),
                          user = new user
                          {
                              userId = (int)orders.Element("user").Element("userId"),
                              email = orders.Element("user").Element("email").Value,
                              fstName = orders.Element("user").Element("fio").Value.Split(' ')[1],
                              sndName = orders.Element("user").Element("fio").Value.Split(' ')[0],
                              trdName = orders.Element("user").Element("fio").Value.Split(' ')[2],
                          }
                      };
            //Подключение к базе
            sqlConnection.Open();
            if (sqlConnection.State == ConnectionState.Open)
            {
                Console.WriteLine("Подключено");
            }

            foreach (order order in ord)
            {   //Вставка Корзин
                SqlCommand insertCart = new SqlCommand($"INSERT INTO Cart (CartId, ClientId)" +
                    $" VALUES (@CartId, @ClientId)", sqlConnection);
                insertCart.Parameters.AddWithValue("CartId", order.no);
                insertCart.Parameters.AddWithValue("ClientId", order.user.userId);
                insertCart.ExecuteNonQuery();

                foreach (product product in order.products)
                {
                    SqlCommand getLastPICtId = new SqlCommand("SELECT MAX(Id) FROM ProductsInCart", sqlConnection);
                    int lastId;
                    try
                    {
                        lastId = (int)getLastPICtId.ExecuteScalar();
                    }
                    catch
                    {
                        lastId = 0;
                    }
                    //Вставка Товаров в корзине
                    SqlCommand insertPIN = new SqlCommand($"INSERT INTO ProductsInCart (Id, ProductId," +
                    $" CartId, Amount) VALUES (@Id, @ProductId, @CartId, @Amount)", sqlConnection);
                    insertPIN.Parameters.AddWithValue("Id", lastId + 1);
                    insertPIN.Parameters.AddWithValue("ProductId", product.productId);
                    insertPIN.Parameters.AddWithValue("CartId", order.no);
                    insertPIN.Parameters.AddWithValue("Amount", product.quantity);
                    insertPIN.ExecuteNonQuery();
                }
                //Вставка истории покупок
                SqlCommand insertHistory = new SqlCommand($"INSERT INTO History (HistoryId, CartId, " +
                    $"RegistrationDate, Total) VALUES (@HistoryId, @CartId, @RegistrationDate, " +
                    $"@Total)", sqlConnection);
                insertHistory.Parameters.AddWithValue("HistoryId", order.orderId);
                insertHistory.Parameters.AddWithValue("CartId", order.no);
                insertHistory.Parameters.AddWithValue("RegistrationDate", order.reg_date);
                insertHistory.Parameters.AddWithValue("Total", order.sum);
                insertHistory.ExecuteNonQuery();
            }
            sqlConnection.Close();
            Console.ReadLine();
        }
    }
}