using Microsoft.EntityFrameworkCore;

namespace TurnosSalud.Api.Models;

/// <summary>
/// El DbContext, "el corazón de EF Core": representa la base TurnosSaludDB.
/// Cada DbSet es una tabla. OnModelCreating dice cómo se llama cada tabla en SQL Server,
/// cuáles son sus claves y cómo se relacionan.
/// </summary>
// =====================================================================
// DbContext = la BASE DE DATOS vista desde C#.
// Cada DbSet es una tabla: db.Medicos equivale a la tabla MEDICO.
// Ejemplo: db.Medicos.Where(m => m.IdEspecialidad == 1)
//          EF Core lo traduce a  SELECT ... FROM MEDICO WHERE IdEspecialidad = 1
// =====================================================================
public partial class TurnosSaludContext : DbContext
{
    // Recibe la cadena de conexión configurada en Program.cs.
    public TurnosSaludContext(DbContextOptions<TurnosSaludContext> options) : base(options)
    {
    }

    public virtual DbSet<ObraSocial> ObrasSociales { get; set; }
    public virtual DbSet<Especialidad> Especialidades { get; set; }
    public virtual DbSet<Sede> Sedes { get; set; }
    public virtual DbSet<Medico> Medicos { get; set; }
    public virtual DbSet<MedicoDia> MedicoDias { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Turno> Turnos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObraSocial>(entity =>
        {
            entity.ToTable("OBRA_SOCIAL");
            entity.HasKey(e => e.IdObraSocial);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Especialidad>(entity =>
        {
            entity.ToTable("ESPECIALIDAD");
            entity.HasKey(e => e.IdEspecialidad);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.Icono).HasMaxLength(40).IsUnicode(false);
            entity.Property(e => e.ColorFondo).HasMaxLength(9).IsUnicode(false);
            entity.Property(e => e.ColorBorde).HasMaxLength(9).IsUnicode(false);
        });

        modelBuilder.Entity<Sede>(entity =>
        {
            entity.ToTable("SEDE");
            entity.HasKey(e => e.IdSede);
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Direccion).HasMaxLength(120);
        });

        modelBuilder.Entity<Medico>(entity =>
        {
            entity.ToTable("MEDICO");
            entity.HasKey(e => e.IdMedico);
            entity.Property(e => e.Titulo).HasMaxLength(5);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.Apellido).HasMaxLength(60);
            entity.Property(e => e.Matricula).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Calificacion).HasColumnType("decimal(2, 1)");
            entity.Property(e => e.HoraInicio).HasColumnType("time(0)");
            entity.Property(e => e.HoraUltimoTurno).HasColumnType("time(0)");
            entity.Property(e => e.ColorAvatar).HasMaxLength(9).IsUnicode(false);

            entity.HasOne(d => d.IdEspecialidadNavigation).WithMany(p => p.Medicos)
                .HasForeignKey(d => d.IdEspecialidad)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Medico_Especialidad");

            entity.HasOne(d => d.IdSedeNavigation).WithMany(p => p.Medicos)
                .HasForeignKey(d => d.IdSede)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Medico_Sede");
        });

        modelBuilder.Entity<MedicoDia>(entity =>
        {
            entity.ToTable("MEDICO_DIA");
            entity.HasKey(e => new { e.IdMedico, e.DiaSemana });   // clave primaria compuesta

            entity.HasOne(d => d.IdMedicoNavigation).WithMany(p => p.Dias)
                .HasForeignKey(d => d.IdMedico)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MedicoDia_Medico");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("USUARIO");
            entity.HasKey(e => e.IdUsuario);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.Apellido).HasMaxLength(60);
            entity.Property(e => e.Dni).HasMaxLength(8).IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(120).IsUnicode(false);
            entity.Property(e => e.Telefono).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.NumeroAfiliado).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.PasswordSalt).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.GoogleId).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            entity.HasIndex(e => e.Dni).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(d => d.IdObraSocialNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdObraSocial)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Usuario_ObraSocial");
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.ToTable("TURNO", tabla => tabla.HasCheckConstraint("CK_Turno_Estado",
                "Estado IN ('Activo','Cancelado','Reprogramado')"));
            entity.HasKey(e => e.IdTurno);
            entity.Property(e => e.Inicio).HasColumnType("datetime2(0)");
            entity.Property(e => e.Estado).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.FechaCancelacion).HasColumnType("datetime2(0)");

            // El índice único filtrado de la base: un horario de un médico, un solo turno activo.
            entity.HasIndex(e => new { e.IdMedico, e.Inicio }).IsUnique()
                .HasFilter("[Estado] = 'Activo'")
                .HasDatabaseName("UX_Turno_Medico_Horario");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Turnos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Turno_Usuario");

            entity.HasOne(d => d.IdMedicoNavigation).WithMany(p => p.Turnos)
                .HasForeignKey(d => d.IdMedico)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Turno_Medico");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
