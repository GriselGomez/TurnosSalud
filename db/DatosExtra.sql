-- =====================================================================
--  DatosExtra.sql - Sedes reales de Formosa Capital
--  Ejecutar DESPUÉS de TurnosSaludDB.sql (o de TurnosSaludDB_hosting.sql).
--  Se puede ejecutar más de una vez sin errores.
-- =====================================================================
USE TurnosSaludDB;   -- en el hosting, borrar esta línea y elegir la base en el desplegable
GO

-- 1) Direcciones y nombres oficiales de las 4 sedes
UPDATE SEDE SET Direccion = N'Salta 550, Formosa'
WHERE Nombre = N'Hospital Central de Formosa';

UPDATE SEDE SET Nombre = N'Hospital de Alta Complejidad "Pte. Juan Domingo Perón"',
                Direccion = N'Av. Dr. Néstor Kirchner y Av. Pantaleón Gómez, Formosa'
WHERE Nombre = N'Hospital de Alta Complejidad';

UPDATE SEDE SET Direccion = N'Córdoba 1450, Formosa'
WHERE Nombre = N'Hospital de la Madre y el Niño';

UPDATE SEDE SET Nombre = N'Hospital Odontológico',
                Direccion = N'Coronel Bogado y Av. 28 de Junio (frente al Hospital Evita), Formosa'
WHERE Nombre = N'Centro Odontológico Formosa';

-- 2) Nueva sede: Hospital Interdistrital Evita (solo si todavía no existe)
IF NOT EXISTS (SELECT 1 FROM SEDE WHERE Nombre = N'Hospital Interdistrital Evita')
    INSERT INTO SEDE (Nombre, Direccion)
    VALUES (N'Hospital Interdistrital Evita', N'Av. 28 de Junio, Formosa');

-- 3) Tres médicos del Central pasan a atender en el Evita
--    (Dr. Diego Morales y Dr. Pablo Romero: Clínica Médica · Dr. Leandro Cáceres: Traumatología)
UPDATE MEDICO
SET IdSede = (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Interdistrital Evita')
WHERE Matricula IN ('MP 11457', 'MP 12890', 'MP 12477');
GO

-- 4) Comprobación: cada sede con su dirección y cuántos médicos tiene
SELECT s.Nombre AS Sede, s.Direccion, COUNT(m.IdMedico) AS Medicos
FROM SEDE s
LEFT JOIN MEDICO m ON m.IdSede = s.IdSede
GROUP BY s.Nombre, s.Direccion
ORDER BY s.Nombre;
