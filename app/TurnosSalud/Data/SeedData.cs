using SQLite;
using TurnosSalud.Helpers;
using TurnosSalud.Models;

namespace TurnosSalud.Data;

/// <summary>
/// Datos de demostración: especialidades, médicos (ficticios) y un paciente de prueba.
/// Se cargan una única vez cuando la base está vacía.
/// </summary>
// Datos iniciales de la versión SQLite. Con la API, estos mismos datos se cargan
// con el script db/TurnosSaludDB.sql. DniDemo y PasswordDemo se siguen usando
// en el recuadro "Cuenta de demostración" del Login.
public static class SeedData
{
    public const string DniDemo = "11222333";
    public const string EmailDemo = "paciente.demo@example.com";
    public const string PasswordDemo = "Turnos2026";

    private const string HospitalCentral = "Hospital Central de Formosa";
    private const string AltaComplejidad = "Hospital de Alta Complejidad";
    private const string MadreNino = "Hospital de la Madre y el Niño";
    private const string Odontologico = "Centro Odontológico Formosa";

    private static readonly string[] ColoresAvatar =
        ["#0B5ED7", "#0E7490", "#7C3AED", "#DB2777", "#16A34A", "#D97706", "#1D4ED8", "#475569"];

    public static async Task CargarSiEstaVaciaAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Especialidad>().CountAsync() > 0)
            return;

        var especialidades = new List<Especialidad>
        {
            new() { Nombre = "Clínica Médica",   Icono = "ic_clinica.png",       ColorFondo = "#EAF2FF", ColorBorde = "#C7DCFF", Orden = 1,  Destacada = true },
            new() { Nombre = "Pediatría",        Icono = "ic_pediatria.png",     ColorFondo = "#E8F7EE", ColorBorde = "#BFE8CF", Orden = 2,  Destacada = true },
            new() { Nombre = "Cardiología",      Icono = "ic_cardiologia.png",   ColorFondo = "#FDECEF", ColorBorde = "#F8C9D1", Orden = 3,  Destacada = true },
            new() { Nombre = "Odontología",      Icono = "ic_odontologia.png",   ColorFondo = "#F1EDFD", ColorBorde = "#D9CFF8", Orden = 4,  Destacada = true },
            new() { Nombre = "Dermatología",     Icono = "ic_dermatologia.png",  ColorFondo = "#FFF6E5", ColorBorde = "#F8E0B0", Orden = 5,  Destacada = true },
            new() { Nombre = "Ginecología",      Icono = "ic_ginecologia.png",   ColorFondo = "#FDEDF5", ColorBorde = "#F6CDE1", Orden = 6 },
            new() { Nombre = "Traumatología",    Icono = "ic_traumatologia.png", ColorFondo = "#E7F5F8", ColorBorde = "#BFE3EA", Orden = 7 },
            new() { Nombre = "Oftalmología",     Icono = "ic_oftalmologia.png",  ColorFondo = "#E8F0FE", ColorBorde = "#C7D8FB", Orden = 8 },
            new() { Nombre = "Nutrición",        Icono = "ic_nutricion.png",     ColorFondo = "#F1F8E6", ColorBorde = "#D6EBB7", Orden = 9 },
            new() { Nombre = "Psicología",       Icono = "ic_psicologia.png",    ColorFondo = "#F3EEFE", ColorBorde = "#DDD0FB", Orden = 10 },
        };
        await db.InsertAllAsync(especialidades);

        int Esp(string nombre) => especialidades.First(e => e.Nombre == nombre).Id;

        var medicos = new List<Medico>
        {
            // Clínica Médica (8)
            M("Dra.", "Mariana", "López",      Esp("Clínica Médica"), "MP 12345", HospitalCentral, 4.8, 124, "1,2,3,4,5", "08:00", "12:00"),
            M("Dr.",  "Carlos",  "Fernández",  Esp("Clínica Médica"), "MP 10872", HospitalCentral, 4.6, 98,  "1,2,3,4,5", "08:00", "12:00"),
            M("Dra.", "Valeria", "Sánchez",    Esp("Clínica Médica"), "MP 13190", AltaComplejidad, 4.9, 210, "1,3,5",     "14:00", "18:00"),
            M("Dr.",  "Diego",   "Morales",    Esp("Clínica Médica"), "MP 11457", HospitalCentral, 4.7, 87,  "2,4",       "08:00", "12:00"),
            M("Dra.", "Lucía",   "Benítez",    Esp("Clínica Médica"), "MP 14022", AltaComplejidad, 4.5, 64,  "1,2,3,4,5", "15:00", "19:00"),
            M("Dr.",  "Martín",  "Acosta",     Esp("Clínica Médica"), "MP 9876",  HospitalCentral, 4.4, 51,  "1,2,3",     "07:30", "11:30"),
            M("Dra.", "Florencia","Giménez",   Esp("Clínica Médica"), "MP 15301", AltaComplejidad, 4.8, 133, "3,4,5",     "09:00", "13:00"),
            M("Dr.",  "Pablo",   "Romero",     Esp("Clínica Médica"), "MP 12890", HospitalCentral, 4.3, 42,  "1,4",       "16:00", "20:00"),

            // Pediatría
            M("Dra.", "Carolina","Ayala",      Esp("Pediatría"), "MP 11220", MadreNino, 4.9, 302, "1,2,3,4,5", "08:00", "12:00"),
            M("Dr.",  "Federico","Ruiz Díaz",  Esp("Pediatría"), "MP 12761", MadreNino, 4.7, 176, "1,3,5",     "14:00", "18:00"),
            M("Dra.", "Natalia", "Cabrera",    Esp("Pediatría"), "MP 13654", MadreNino, 4.8, 145, "2,4",       "08:00", "12:00"),

            // Cardiología
            M("Dr.",  "Javier",  "Ojeda",      Esp("Cardiología"), "MP 10233", AltaComplejidad, 4.8, 156, "1,2,3,4,5", "08:00", "12:00"),
            M("Dra.", "Silvina", "Duarte",     Esp("Cardiología"), "MP 11798", AltaComplejidad, 4.6, 92,  "2,4",       "14:00", "18:00"),

            // Odontología
            M("Dra.", "Paula",   "Villalba",   Esp("Odontología"), "MP 14510", Odontologico, 4.7, 118, "1,2,3,4,5", "08:00", "12:00"),
            M("Dr.",  "Hernán",  "Galeano",    Esp("Odontología"), "MP 13377", Odontologico, 4.5, 77,  "1,3,5",     "15:00", "19:00"),

            // Dermatología
            M("Dra.", "Romina",  "Espínola",   Esp("Dermatología"), "MP 12099", HospitalCentral, 4.8, 103, "2,3,4", "09:00", "13:00"),
            M("Dr.",  "Gustavo", "Medina",     Esp("Dermatología"), "MP 10655", AltaComplejidad, 4.4, 58,  "1,5",   "14:00", "18:00"),

            // Ginecología
            M("Dra.", "Andrea",  "Insaurralde",Esp("Ginecología"), "MP 11903", MadreNino, 4.9, 187, "1,2,3,4,5", "08:00", "12:00"),
            M("Dra.", "Cecilia", "Paniagua",   Esp("Ginecología"), "MP 13045", MadreNino, 4.6, 81,  "2,4",       "14:00", "18:00"),

            // Traumatología
            M("Dr.",  "Sergio",  "Aquino",     Esp("Traumatología"), "MP 10541", AltaComplejidad, 4.7, 134, "1,3,5", "08:00", "12:00"),
            M("Dr.",  "Leandro", "Cáceres",    Esp("Traumatología"), "MP 12477", HospitalCentral, 4.5, 69,  "2,4",   "15:00", "19:00"),

            // Oftalmología
            M("Dra.", "Mónica",  "Báez",       Esp("Oftalmología"), "MP 11186", HospitalCentral, 4.8, 121, "1,2,3", "08:00", "12:00"),
            M("Dr.",  "Raúl",    "Ortiz",      Esp("Oftalmología"), "MP 9932",  AltaComplejidad, 4.6, 88, "4,5",   "09:00", "13:00"),

            // Nutrición
            M("Lic.", "Agustina","Vera",       Esp("Nutrición"), "MP 2210", HospitalCentral, 4.9, 97, "1,3,5", "09:00", "13:00"),

            // Psicología
            M("Lic.", "Tomás",   "Quintana",   Esp("Psicología"), "MP 1874", HospitalCentral, 4.8, 66, "2,4", "14:00", "18:00"),
            M("Lic.", "Julieta", "Rolón",      Esp("Psicología"), "MP 2045", MadreNino,       4.7, 54, "1,3,5", "08:00", "12:00"),
        };

        for (int i = 0; i < medicos.Count; i++)
            medicos[i].ColorAvatar = ColoresAvatar[i % ColoresAvatar.Length];

        await db.InsertAllAsync(medicos);

        // Paciente de demostración (podés registrar todos los que quieras desde la app).
        var (hash, salt) = PasswordHasher.Hash(PasswordDemo);
        await db.InsertAsync(new Usuario
        {
            Nombre = "Griselda Elizabeth",
            Apellido = "Gómez",
            Dni = DniDemo,
            Email = EmailDemo,
            Telefono = "3704000000",
            ObraSocial = "IASEP",
            NumeroAfiliado = string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            FechaRegistro = DateTime.Now
        });
    }

    private static Medico M(string titulo, string nombre, string apellido, int especialidadId, string matricula,
        string sede, double calificacion, int resenas, string dias, string desde, string ultimo) => new()
    {
        Titulo = titulo,
        Nombre = nombre,
        Apellido = apellido,
        EspecialidadId = especialidadId,
        Matricula = matricula,
        Sede = sede,
        Calificacion = calificacion,
        CantidadResenas = resenas,
        DiasAtencion = dias,
        HoraInicio = desde,
        HoraUltimoTurno = ultimo,
        DuracionTurnoMinutos = 30
    };
}
