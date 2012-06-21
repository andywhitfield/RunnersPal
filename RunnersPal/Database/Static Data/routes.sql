insert into UserAccount(DisplayName, CreatedDate, LastActivityDate, EmailAddress, OriginalHostAddress, UserType, DistanceUnits)
values ('Admin', GETDATE(), GETDATE(), 'admin@nosuchblogger.com', 'runnerspal.nosuchblogger.com', 'A', 0)

declare @adminid int
select @adminid = id from UserAccount where DisplayName = 'Admin' and UserType = 'A'

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('1 Kilometer', 1, 1, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('1 Mile', 1, 0, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('3 Miles', 3, 0, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('5 Kilometers', 5, 1, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('10 Kilometers', 10, 1, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('10 Miles', 10, 0, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('Half-marathon', 13.109375, 0, @adminid, GETDATE())
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate)
values ('Marathon', 26.21875, 0, @adminid, GETDATE())
