UPDATE wuarike_db."AspNetUsers" SET "AccessFailedCount"=0, "LockoutEnd"=NULL, "PasswordHash"='AQAAAAEAACcQAAAAEKcBgHTgTk33aMWjil/nrZUN1uV+QAnwS8qj7wj1r3N+5q0m8pwqPNwrsAxri7d3cQ==', "SecurityStamp"='seed-6.0.5-stamp'
WHERE "UserName"='ADMIN';
SELECT "UserName", "AccessFailedCount", left("PasswordHash",20) AS hash_prefix FROM wuarike_db."AspNetUsers";
