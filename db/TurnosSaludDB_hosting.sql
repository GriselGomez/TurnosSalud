-- =====================================================================
--  TurnosSaludDB - versión para el HOSTING (MonsterASP.NET)
--  En el hosting la base ya existe (se crea desde el panel), por eso este
--  script NO crea la base: solo crea las tablas y carga los datos.
--  Ejecutarlo conectado a la base del hosting (en SSMS, elegir esa base arriba).
--  Si las tablas ya existen, las borra y las vuelve a crear.
-- =====================================================================
IF OBJECT_ID('TURNO') IS NOT NULL DROP TABLE TURNO;
IF OBJECT_ID('USUARIO') IS NOT NULL DROP TABLE USUARIO;
IF OBJECT_ID('MEDICO_DIA') IS NOT NULL DROP TABLE MEDICO_DIA;
IF OBJECT_ID('MEDICO') IS NOT NULL DROP TABLE MEDICO;
IF OBJECT_ID('SEDE') IS NOT NULL DROP TABLE SEDE;
IF OBJECT_ID('ESPECIALIDAD') IS NOT NULL DROP TABLE ESPECIALIDAD;
IF OBJECT_ID('OBRA_SOCIAL') IS NOT NULL DROP TABLE OBRA_SOCIAL;
GO

-- ---------- Tablas de catálogo ----------
CREATE TABLE OBRA_SOCIAL (
    IdObraSocial INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       NVARCHAR(60) NOT NULL UNIQUE
);

CREATE TABLE ESPECIALIDAD (
    IdEspecialidad INT IDENTITY(1,1) PRIMARY KEY,
    Nombre     NVARCHAR(60) NOT NULL UNIQUE,
    Icono      VARCHAR(40)  NULL,
    ColorFondo VARCHAR(9)   NULL,
    ColorBorde VARCHAR(9)   NULL,
    Orden      INT          NOT NULL DEFAULT 0,
    Destacada  BIT          NOT NULL DEFAULT 0
);

CREATE TABLE SEDE (
    IdSede    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre    NVARCHAR(80)  NOT NULL UNIQUE,
    Direccion NVARCHAR(120) NULL
);

-- ---------- Médicos y sus días de atención ----------
CREATE TABLE MEDICO (
    IdMedico        INT IDENTITY(1,1) PRIMARY KEY,
    Titulo          NVARCHAR(5)  NOT NULL DEFAULT N'Dr.',
    Nombre          NVARCHAR(60) NOT NULL,
    Apellido        NVARCHAR(60) NOT NULL,
    IdEspecialidad  INT NOT NULL CONSTRAINT FK_Medico_Especialidad REFERENCES ESPECIALIDAD(IdEspecialidad),
    IdSede          INT NOT NULL CONSTRAINT FK_Medico_Sede REFERENCES SEDE(IdSede),
    Matricula       VARCHAR(20)  NOT NULL UNIQUE,
    Calificacion    DECIMAL(2,1) NOT NULL DEFAULT 0,
    CantidadResenas INT          NOT NULL DEFAULT 0,
    HoraInicio      TIME(0)      NOT NULL,   -- primer turno del día
    HoraUltimoTurno TIME(0)      NOT NULL,   -- último turno del día
    DuracionTurnoMinutos INT     NOT NULL DEFAULT 30 CHECK (DuracionTurnoMinutos BETWEEN 10 AND 120),
    ColorAvatar     VARCHAR(9)   NULL
);

CREATE TABLE MEDICO_DIA (
    IdMedico  INT     NOT NULL CONSTRAINT FK_MedicoDia_Medico REFERENCES MEDICO(IdMedico),
    DiaSemana TINYINT NOT NULL CHECK (DiaSemana BETWEEN 0 AND 6),   -- 0 = domingo ... 6 = sábado
    CONSTRAINT PK_MedicoDia PRIMARY KEY (IdMedico, DiaSemana)
);

-- ---------- Pacientes ----------
CREATE TABLE USUARIO (
    IdUsuario      INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         NVARCHAR(60)  NOT NULL,
    Apellido       NVARCHAR(60)  NOT NULL,
    Dni            VARCHAR(8)    NOT NULL UNIQUE,
    Email          VARCHAR(120)  NOT NULL UNIQUE,
    Telefono       VARCHAR(10)   NOT NULL DEFAULT '',
    IdObraSocial   INT NOT NULL CONSTRAINT FK_Usuario_ObraSocial REFERENCES OBRA_SOCIAL(IdObraSocial),
    NumeroAfiliado VARCHAR(30)   NOT NULL DEFAULT '',
    PasswordHash   VARCHAR(100)  NOT NULL,
    PasswordSalt   VARCHAR(50)   NOT NULL,
    GoogleId       VARCHAR(64)   NULL,
    FechaRegistro  DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME()
);

-- Un GoogleId puede quedar vacío, pero si se carga no se repite.
CREATE UNIQUE INDEX UX_Usuario_GoogleId ON USUARIO(GoogleId) WHERE GoogleId IS NOT NULL;

-- ---------- Turnos ----------
CREATE TABLE TURNO (
    IdTurno          INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario        INT NOT NULL CONSTRAINT FK_Turno_Usuario REFERENCES USUARIO(IdUsuario),
    IdMedico         INT NOT NULL CONSTRAINT FK_Turno_Medico  REFERENCES MEDICO(IdMedico),
    Inicio           DATETIME2(0) NOT NULL,
    Estado           VARCHAR(15)  NOT NULL DEFAULT 'Activo'
                     CONSTRAINT CK_Turno_Estado CHECK (Estado IN ('Activo','Cancelado','Reprogramado')),
    FechaCreacion    DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    FechaCancelacion DATETIME2(0) NULL
);

-- La base impide la doble reserva: un médico no puede tener dos turnos ACTIVOS en el mismo horario.
CREATE UNIQUE INDEX UX_Turno_Medico_Horario ON TURNO(IdMedico, Inicio) WHERE Estado = 'Activo';
-- Acelera "mis turnos".
CREATE INDEX IX_Turno_Usuario ON TURNO(IdUsuario, Inicio);
GO

-- =====================================================================
--  Datos iniciales (los mismos de Data/SeedData.cs de la app)
-- =====================================================================

INSERT INTO OBRA_SOCIAL (Nombre) VALUES
    (N'Particular (sin obra social)'),
    (N'IASEP'),
    (N'PAMI'),
    (N'OSDE'),
    (N'Swiss Medical'),
    (N'Galeno'),
    (N'Sancor Salud'),
    (N'OSECAC'),
    (N'OSPRERA'),
    (N'IOSFA'),
    (N'Medifé'),
    (N'Otra');

INSERT INTO ESPECIALIDAD (Nombre, Icono, ColorFondo, ColorBorde, Orden, Destacada) VALUES
    (N'Clínica Médica', 'ic_clinica.png', '#EAF2FF', '#C7DCFF', 1, 1),
    (N'Pediatría', 'ic_pediatria.png', '#E8F7EE', '#BFE8CF', 2, 1),
    (N'Cardiología', 'ic_cardiologia.png', '#FDECEF', '#F8C9D1', 3, 1),
    (N'Odontología', 'ic_odontologia.png', '#F1EDFD', '#D9CFF8', 4, 1),
    (N'Dermatología', 'ic_dermatologia.png', '#FFF6E5', '#F8E0B0', 5, 1),
    (N'Ginecología', 'ic_ginecologia.png', '#FDEDF5', '#F6CDE1', 6, 0),
    (N'Traumatología', 'ic_traumatologia.png', '#E7F5F8', '#BFE3EA', 7, 0),
    (N'Oftalmología', 'ic_oftalmologia.png', '#E8F0FE', '#C7D8FB', 8, 0),
    (N'Nutrición', 'ic_nutricion.png', '#F1F8E6', '#D6EBB7', 9, 0),
    (N'Psicología', 'ic_psicologia.png', '#F3EEFE', '#DDD0FB', 10, 0);

INSERT INTO SEDE (Nombre) VALUES
    (N'Hospital Central de Formosa'),
    (N'Hospital de Alta Complejidad'),
    (N'Hospital de la Madre y el Niño'),
    (N'Centro Odontológico Formosa');

-- Médicos (nombres ficticios). La especialidad y la sede se buscan por nombre.
INSERT INTO MEDICO (Titulo, Nombre, Apellido, IdEspecialidad, IdSede, Matricula, Calificacion, CantidadResenas, HoraInicio, HoraUltimoTurno, DuracionTurnoMinutos, ColorAvatar)
SELECT N'Dra.', N'Mariana', N'López', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 12345', 4.8, 124, '08:00', '12:00', 30, '#0B5ED7'
UNION ALL SELECT N'Dr.', N'Carlos', N'Fernández', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 10872', 4.6, 98, '08:00', '12:00', 30, '#0E7490'
UNION ALL SELECT N'Dra.', N'Valeria', N'Sánchez', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 13190', 4.9, 210, '14:00', '18:00', 30, '#7C3AED'
UNION ALL SELECT N'Dr.', N'Diego', N'Morales', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 11457', 4.7, 87, '08:00', '12:00', 30, '#DB2777'
UNION ALL SELECT N'Dra.', N'Lucía', N'Benítez', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 14022', 4.5, 64, '15:00', '19:00', 30, '#16A34A'
UNION ALL SELECT N'Dr.', N'Martín', N'Acosta', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 9876', 4.4, 51, '07:30', '11:30', 30, '#D97706'
UNION ALL SELECT N'Dra.', N'Florencia', N'Giménez', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 15301', 4.8, 133, '09:00', '13:00', 30, '#1D4ED8'
UNION ALL SELECT N'Dr.', N'Pablo', N'Romero', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Clínica Médica'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 12890', 4.3, 42, '16:00', '20:00', 30, '#475569'
UNION ALL SELECT N'Dra.', N'Carolina', N'Ayala', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Pediatría'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 11220', 4.9, 302, '08:00', '12:00', 30, '#0B5ED7'
UNION ALL SELECT N'Dr.', N'Federico', N'Ruiz Díaz', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Pediatría'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 12761', 4.7, 176, '14:00', '18:00', 30, '#0E7490'
UNION ALL SELECT N'Dra.', N'Natalia', N'Cabrera', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Pediatría'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 13654', 4.8, 145, '08:00', '12:00', 30, '#7C3AED'
UNION ALL SELECT N'Dr.', N'Javier', N'Ojeda', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Cardiología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 10233', 4.8, 156, '08:00', '12:00', 30, '#DB2777'
UNION ALL SELECT N'Dra.', N'Silvina', N'Duarte', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Cardiología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 11798', 4.6, 92, '14:00', '18:00', 30, '#16A34A'
UNION ALL SELECT N'Dra.', N'Paula', N'Villalba', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Odontología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Centro Odontológico Formosa'), 'MP 14510', 4.7, 118, '08:00', '12:00', 30, '#D97706'
UNION ALL SELECT N'Dr.', N'Hernán', N'Galeano', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Odontología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Centro Odontológico Formosa'), 'MP 13377', 4.5, 77, '15:00', '19:00', 30, '#1D4ED8'
UNION ALL SELECT N'Dra.', N'Romina', N'Espínola', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Dermatología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 12099', 4.8, 103, '09:00', '13:00', 30, '#475569'
UNION ALL SELECT N'Dr.', N'Gustavo', N'Medina', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Dermatología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 10655', 4.4, 58, '14:00', '18:00', 30, '#0B5ED7'
UNION ALL SELECT N'Dra.', N'Andrea', N'Insaurralde', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Ginecología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 11903', 4.9, 187, '08:00', '12:00', 30, '#0E7490'
UNION ALL SELECT N'Dra.', N'Cecilia', N'Paniagua', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Ginecología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 13045', 4.6, 81, '14:00', '18:00', 30, '#7C3AED'
UNION ALL SELECT N'Dr.', N'Sergio', N'Aquino', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Traumatología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 10541', 4.7, 134, '08:00', '12:00', 30, '#DB2777'
UNION ALL SELECT N'Dr.', N'Leandro', N'Cáceres', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Traumatología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 12477', 4.5, 69, '15:00', '19:00', 30, '#16A34A'
UNION ALL SELECT N'Dra.', N'Mónica', N'Báez', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Oftalmología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 11186', 4.8, 121, '08:00', '12:00', 30, '#D97706'
UNION ALL SELECT N'Dr.', N'Raúl', N'Ortiz', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Oftalmología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de Alta Complejidad'), 'MP 9932', 4.6, 88, '09:00', '13:00', 30, '#1D4ED8'
UNION ALL SELECT N'Lic.', N'Agustina', N'Vera', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Nutrición'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 2210', 4.9, 97, '09:00', '13:00', 30, '#475569'
UNION ALL SELECT N'Lic.', N'Tomás', N'Quintana', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Psicología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital Central de Formosa'), 'MP 1874', 4.8, 66, '14:00', '18:00', 30, '#0B5ED7'
UNION ALL SELECT N'Lic.', N'Julieta', N'Rolón', (SELECT IdEspecialidad FROM ESPECIALIDAD WHERE Nombre = N'Psicología'), (SELECT IdSede FROM SEDE WHERE Nombre = N'Hospital de la Madre y el Niño'), 'MP 2045', 4.7, 54, '08:00', '12:00', 30, '#0E7490';

-- Días de atención: un día por fila (1 = lunes ... 5 = viernes).
INSERT INTO MEDICO_DIA (IdMedico, DiaSemana)
SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 12345'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 12345'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 12345'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 12345'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 12345'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 10872'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 10872'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 10872'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 10872'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 10872'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 13190'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 13190'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 13190'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 11457'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 11457'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 14022'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 14022'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 14022'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 14022'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 14022'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 9876'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 9876'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 9876'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 15301'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 15301'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 15301'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 12890'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 12890'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 11220'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 11220'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 11220'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 11220'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 11220'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 12761'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 12761'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 12761'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 13654'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 13654'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 10233'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 10233'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 10233'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 10233'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 10233'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 11798'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 11798'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 14510'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 14510'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 14510'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 14510'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 14510'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 13377'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 13377'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 13377'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 12099'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 12099'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 12099'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 10655'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 10655'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 11903'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 11903'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 11903'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 11903'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 11903'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 13045'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 13045'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 10541'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 10541'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 10541'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 12477'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 12477'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 11186'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 11186'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 11186'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 9932'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 9932'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 2210'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 2210'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 2210'
UNION ALL SELECT IdMedico, 2 FROM MEDICO WHERE Matricula = 'MP 1874'
UNION ALL SELECT IdMedico, 4 FROM MEDICO WHERE Matricula = 'MP 1874'
UNION ALL SELECT IdMedico, 1 FROM MEDICO WHERE Matricula = 'MP 2045'
UNION ALL SELECT IdMedico, 3 FROM MEDICO WHERE Matricula = 'MP 2045'
UNION ALL SELECT IdMedico, 5 FROM MEDICO WHERE Matricula = 'MP 2045';

-- Paciente de prueba: DNI 11222333, contraseña Turnos2026 (guardada como hash PBKDF2-SHA256 con salt).
INSERT INTO USUARIO (Nombre, Apellido, Dni, Email, Telefono, IdObraSocial, NumeroAfiliado, PasswordHash, PasswordSalt)
VALUES (N'Griselda Elizabeth', N'Gómez', '11222333', 'paciente.demo@example.com', '3704000000',
        (SELECT IdObraSocial FROM OBRA_SOCIAL WHERE Nombre = N'IASEP'), '',
        'cFbKksvmEXc3xvy5bwyQaYZV1LZzOoqrP/eFTwdwSEU=', '8h/JpqNDAXLqDrGQ8omsOg==');
GO

-- Comprobación rápida: deben verse 12, 10, 4, 26, 83 y 1 filas.
SELECT 'OBRA_SOCIAL' AS Tabla, COUNT(*) AS Filas FROM OBRA_SOCIAL
UNION ALL SELECT 'ESPECIALIDAD', COUNT(*) FROM ESPECIALIDAD
UNION ALL SELECT 'SEDE', COUNT(*) FROM SEDE
UNION ALL SELECT 'MEDICO', COUNT(*) FROM MEDICO
UNION ALL SELECT 'MEDICO_DIA', COUNT(*) FROM MEDICO_DIA
UNION ALL SELECT 'USUARIO', COUNT(*) FROM USUARIO;
