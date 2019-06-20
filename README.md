# Sample MassTransit Quartz Scheduler

This sample contains three examples:

1. Net461 using TopShelf
2. NetCore Win Svc (for still deploying on a Windows Server)
3. NetCore (linux, windows, mac...)

The persistence mechanism used in this example is SQLServer, however Quartz.net supports [all of these](https://github.com/quartznet/quartznet/tree/master/database/tables).

## Building

Connect to your development SQLServer, windows users is most likely (localdb)\MSSQLLocalDb, and [run this script](create_quartz_tables.sql)

Open the .sln, and run any one of the three projects. Done, you have a MT Scheduler!