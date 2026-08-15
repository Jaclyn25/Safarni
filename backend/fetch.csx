using System;
using System.Data.SqlClient;

var conn = new SqlConnection("Server=localhost;Database=Safarni;Trusted_Connection=True;Encrypt=False");
conn.Open();
var cmd = new SqlCommand("SELECT TOP 1 ItineraryJson FROM SnapshotVersions WHERE Id = 'bbe1a536-eeb5-4bcd-aba8-1067dd587c54'", conn);
var reader = cmd.ExecuteReader();
if (reader.Read()) {
    Console.WriteLine(reader.GetString(0));
}
