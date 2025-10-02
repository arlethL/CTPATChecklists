-- 1) Guarda en variables los Ids de tu usuario y del rol Administrador
DECLARE @userId NVARCHAR(450) = (
  SELECT Id 
  FROM AspNetUsers 
  WHERE Email = 'admin@ctpat.com'
);

DECLARE @roleId NVARCHAR(450) = (
  SELECT Id 
  FROM AspNetRoles 
  WHERE Name = 'Administrador'
);

-- 2) Inserta el registro en AspNetUserRoles si no existe
IF NOT EXISTS (
  SELECT 1 
  FROM AspNetUserRoles 
  WHERE UserId = @userId 
    AND RoleId = @roleId
)
BEGIN
  INSERT INTO AspNetUserRoles (UserId, RoleId)
  VALUES (@userId, @roleId);
END
