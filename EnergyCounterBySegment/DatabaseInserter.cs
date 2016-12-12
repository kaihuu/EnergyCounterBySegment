using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace EnergyCounterBySegment
{
    public class DatabaseInserter
    {
        public static void InsertGidsDifference(List<GidsDifferenceData> result)
        {
            string cn = @"Data Source=ECOLOGDB;Initial Catalog=ECOLOGDBver2;Integrated Security=True";//接続DB

            String insertString;
            for (int i = 0; i < result.Count; i++)
            {
                insertString = makeQueryGidsDifference(result[i]);

                using (SqlConnection sqlConnection = new SqlConnection(cn))
                {
                    sqlConnection.Open();
                    SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                    SqlCommand sqlCommand = sqlConnection.CreateCommand();
                    sqlCommand.Transaction = sqlTransaction;
                    try
                    {
                        sqlCommand.CommandText = insertString;
                        sqlCommand.ExecuteNonQuery();
                        sqlTransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
            }
        }
        public static void InsertConsumedElectricEnergy(List<ConsumedElectricEnergyData> result)
        {
            string cn = @"Data Source=ECOLOGDB;Initial Catalog=ECOLOGDBver2;Integrated Security=True";//接続DB

            String insertString;
            for (int i = 0; i < result.Count; i++)
            {
                insertString = makeQueryConsumedElectricEnergy(result[i]);

                using (SqlConnection sqlConnection = new SqlConnection(cn))
                {
                    sqlConnection.Open();
                    SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                    SqlCommand sqlCommand = sqlConnection.CreateCommand();
                    sqlCommand.Transaction = sqlTransaction;
                    try
                    {
                        sqlCommand.CommandText = insertString;
                        sqlCommand.ExecuteNonQuery();
                        sqlTransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
            }
        }
        public static string makeQueryGidsDifference (GidsDifferenceData data)
        {
            string query = "INSERT INTO GIDS_DIFFERENCE_100M_SEGMENT VALUES ('" + data.segmentId + "','" + data.semanticLinkId + "','" + data.tripId + "','";
            query += data.jst + "','" + data.gidsDifference + "')";

            return query;
        }
        public static string makeQueryConsumedElectricEnergy(ConsumedElectricEnergyData data)
        {
            string query = "INSERT INTO CONSUMED_ELECTRIC_ENERGY_100M_SEGMENT VALUES ('" + data.segmentId + "','" + data.semanticLinkId + "','" + data.tripId + "','";
            query += data.jst + "','" + data.consumedElectricEnergy + "')";

            return query;
        }
    }
}
