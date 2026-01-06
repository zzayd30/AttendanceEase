select * from __EFMigrationsHistory
select * from AspNetRoleClaims
select * from AspNetRoles
select * from AspNetUserLogins
select * from AspNetUserClaims
select * from AspNetUserRoles
select * from AspNetUsers
select * from AspNetUserTokens
select * from Batches
select * from Sections
select * from Students

delete from AspNetUsers where Id = '811a8366-785f-4a34-ac95-a335fbd2664e';

UPDATE AspNetUsers
SET PasswordHash = 'AQAAAAIAAYagAAAAENuIe57NOEsUEfmTyT5AfGaVx/wZwNs/RRn0VERsJ/BL3VRWLdS69wwkt1IKvZfRyQ=='
WHERE Id = 'c6dfafa1-d316-4176-8d88-30f3f03e88f8';
