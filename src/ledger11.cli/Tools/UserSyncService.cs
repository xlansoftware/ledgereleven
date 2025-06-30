using System;
using System.Data;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

public class UserSyncService
{
    public static void SyncAspNetUsers(string sourceDb, string targetDb, ILogger? logger = null)
    {
        string sourceConnStr = $"Data Source={sourceDb};Version=3;";
        string targetConnStr = $"Data Source={targetDb};Version=3;";

        using var sourceConn = new SQLiteConnection(sourceConnStr);
        using var targetConn = new SQLiteConnection(targetConnStr);

        sourceConn.Open();
        targetConn.Open();

        // Read users from source
        string selectQuery = "SELECT * FROM AspNetUsers;";
        using var selectCmd = new SQLiteCommand(selectQuery, sourceConn);
        using var reader = selectCmd.ExecuteReader();

        while (reader.Read())
        {
            string userName = reader["UserName"].ToString()!;
            string id = reader["Id"].ToString()!; // source Id, ignored for insert/update
            var columnValues = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                columnValues[colName] = reader.GetValue(i);
            }

            // Check if user exists in target (by UserName)
            string checkQuery = "SELECT Id FROM AspNetUsers WHERE LOWER(UserName) = LOWER(@UserName);";
            using var checkCmd = new SQLiteCommand(checkQuery, targetConn);
            checkCmd.Parameters.AddWithValue("@UserName", userName);
            var existingUserId = checkCmd.ExecuteScalar();

            if (existingUserId != null)
            {
                logger?.LogInformation("Updating user: {UserName}", userName);

                // UPDATE
                string updateQuery = BuildUpdateQuery("AspNetUsers", columnValues, "UserName");
                using var updateCmd = new SQLiteCommand(updateQuery, targetConn);
                foreach (var kvp in columnValues)
                    updateCmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value);
                updateCmd.ExecuteNonQuery();
            }
            else
            {
                logger?.LogInformation("Inserting user: {UserName}", userName);
                
                // INSERT
                string insertQuery = BuildInsertQuery("AspNetUsers", columnValues);
                using var insertCmd = new SQLiteCommand(insertQuery, targetConn);
                foreach (var kvp in columnValues)
                    insertCmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value);
                insertCmd.ExecuteNonQuery();
            }
        }
    }

    private static string BuildInsertQuery(string tableName, Dictionary<string, object> columns)
    {
        string colNames = string.Join(", ", columns.Keys);
        string paramNames = string.Join(", ", columns.Keys.Select(k => "@" + k));
        return $"INSERT INTO {tableName} ({colNames}) VALUES ({paramNames});";
    }

    private static string BuildUpdateQuery(string tableName, Dictionary<string, object> columns, string whereColumn)
    {
        var assignments = string.Join(", ", columns.Where(kvp => kvp.Key != whereColumn).Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
        return $"UPDATE {tableName} SET {assignments} WHERE LOWER({whereColumn}) = LOWER(@{whereColumn});";
    }
}
