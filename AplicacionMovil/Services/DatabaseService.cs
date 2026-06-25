using AplicacionMovil.Core.Models;
using AplicacionMovil.Modules.Deficiencias.Data;
using AplicacionMovil.Modules.Reclamos.Data;
using Microsoft.Maui.Storage;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AplicacionMovil.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "movil_elpu.db3");
        }

        // ================== INICIALIZAR BASE DE DATOS ==================
        private async Task Init()
        {
            if (_database != null) return;

            _database = new SQLiteAsyncConnection(_dbPath);

            // ✅ Tablas Deficiencias
            await _database.CreateTableAsync<EjecucionDeficiencia>();
            await _database.CreateTableAsync<FotoDeficiencia>();

            // ✅ Tablas Reclamos (OT)
            await _database.CreateTableAsync<EjecucionReclamoOt>();
            await _database.CreateTableAsync<FotoReclamoOt>();

            // De inspecciones
            await _database.CreateTableAsync<InspeccionDeficiencia>();
        }

        // ================== DEFICIENCIAS ==================

        public async Task<EjecucionDeficiencia?> ObtenerEjecucionPorCodigoAsync(string codigoDeficiencia)
        {
            try
            {
                await Init();
                return await _database!.Table<EjecucionDeficiencia>()
                    .Where(e => e.CodigoDeficiencia == codigoDeficiencia)
                    .OrderByDescending(e => e.FechaCreacion)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<FotoDeficiencia>> ObtenerFotosPorEjecucionAsync(int idEjecucion)
        {
            try
            {
                await Init();
                return await _database!.Table<FotoDeficiencia>()
                    .Where(f => f.IdEjecucion == idEjecucion)
                    .ToListAsync();
            }
            catch
            {
                return new List<FotoDeficiencia>();
            }
        }

        public async Task<int> GuardarEjecucionOfflineAsync(EjecucionDeficiencia ejecucion)
        {
            try
            {
                await Init();

                // ✅ Asegura usuario
                ejecucion.UsuarioEjecucion = Preferences.Get("UserName", ejecucion.UsuarioEjecucion ?? "");
                ejecucion.Sincronizado = false;
                ejecucion.FechaCreacion = DateTime.Now;

                await _database!.InsertAsync(ejecucion);
                var idReal = ejecucion.Id;

                // Fotos asociadas (rutas)
                if (ejecucion.Fotos != null && ejecucion.Fotos.Any())
                {
                    foreach (var ruta in ejecucion.Fotos.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        await _database.InsertAsync(new FotoDeficiencia
                        {
                            IdEjecucion = idReal,
                            RutaLocal = ruta,
                            Sincronizada = false,
                            FechaCaptura = DateTime.Now
                        });
                    }
                }

                return idReal;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> MarcarEjecucionComoSincronizadaAsync(int idEjecucion)
        {
            try
            {
                await Init();

                var ejec = await _database!.Table<EjecucionDeficiencia>()
                    .Where(e => e.Id == idEjecucion)
                    .FirstOrDefaultAsync();

                if (ejec == null) return 0;

                ejec.Sincronizado = true;
                return await _database.UpdateAsync(ejec);
            }
            catch
            {
                return 0;
            }
        }

        // ================== RECLAMOS (OT) ==================

        public async Task<int> GuardarReclamoOtOfflineAsync(EjecucionReclamoOt e)
        {
            try
            {
                await Init();

                // ✅ Asegura usuario
                e.UsuarioEjecucion = Preferences.Get("UserName", e.UsuarioEjecucion ?? e.Usuario ?? "");
                e.FechaCreacion = DateTime.Now;

                await _database!.InsertAsync(e);
                return e.Id;
            }
            catch
            {
                return 0;
            }
        }

        public async Task GuardarFotoReclamoOtAsync(int ejecucionId, string ruta)
        {
            await Init();

            if (string.IsNullOrWhiteSpace(ruta)) return;

            await _database!.InsertAsync(new FotoReclamoOt
            {
                IdEjecucion = ejecucionId,
                RutaLocal = ruta,
                Sincronizada = false,
                FechaCaptura = DateTime.Now
            });
        }

        public async Task<EjecucionReclamoOt?> ObtenerReclamoOtPorCodigoAsync(string codigoReclamo)
        {
            try
            {
                await Init();
                return await _database!.Table<EjecucionReclamoOt>()
                    .Where(x => x.CodigoReclamo == codigoReclamo)
                    .OrderByDescending(x => x.FechaCreacion)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<FotoReclamoOt>> ObtenerFotosReclamoOtAsync(int ejecucionId)
        {
            try
            {
                await Init();
                return await _database!.Table<FotoReclamoOt>()
                    .Where(f => f.IdEjecucion == ejecucionId)
                    .ToListAsync();
            }
            catch
            {
                return new List<FotoReclamoOt>();
            }
        }

        public async Task<int> MarcarReclamoOtComoSincronizadoAsync(int ejecucionId)
        {
            try
            {
                await Init();
                var e = await _database!.Table<EjecucionReclamoOt>()
                    .Where(x => x.Id == ejecucionId)
                    .FirstOrDefaultAsync();

                if (e == null) return 0;

                e.Sincronizado = true;
                return await _database.UpdateAsync(e);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<EjecucionDeficiencia>> ObtenerTodasLasEjecucionesPorUsuarioAsync(string userName)
        {
            await Init();

            return await _database.Table<EjecucionDeficiencia>()
                .Where(e => e.UsuarioEjecucion == userName)
                .OrderByDescending(e => e.FechaEjecucion)
                .ToListAsync();
        }

        public async Task<int> EliminarEjecucionAsync(int idEjecucion)
        {
            await Init();

            var fotos = await _database.Table<FotoDeficiencia>()
                .Where(f => f.IdEjecucion == idEjecucion)
                .ToListAsync();

            foreach (var foto in fotos)
                await _database.DeleteAsync(foto);

            return await _database.DeleteAsync<EjecucionDeficiencia>(idEjecucion);
        }

        /// <summary>
        /// Elimina un reclamo/OT de la tabla de reclamos pendientes
        /// después de ser atendido y enviado exitosamente
        /// </summary>
        public async Task EliminarReclamoPendienteAsync(string codigoReclamo)
        {
            try
            {
                await Init();

                var items = await _database!.Table<EjecucionReclamoOt>()
                    .Where(x => x.CodigoReclamo == codigoReclamo)
                    .ToListAsync();

                foreach (var item in items)
                {
                    var fotos = await _database.Table<FotoReclamoOt>()
                        .Where(f => f.IdEjecucion == item.Id)
                        .ToListAsync();

                    foreach (var foto in fotos)
                        await _database.DeleteAsync(foto);

                    await _database.DeleteAsync(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar reclamo pendiente: {ex.Message}");
            }
        }

        public async Task<int> GuardarInspeccionAsync(InspeccionDeficiencia item)
        {
            await Init();
            return await _database!.InsertAsync(item);
        }

        public async Task<List<InspeccionDeficiencia>> ListarInspeccionesAsync()
        {
            await Init();
            return await _database!.Table<InspeccionDeficiencia>()
                .OrderByDescending(x => x.FechaRegistroLocal)
                .ToListAsync();
        }

        public async Task MarcarInspeccionSincronizadaAsync(int idLocal, long idServidor)
        {
            await Init();
            var row = await _database!.Table<InspeccionDeficiencia>()
                .FirstOrDefaultAsync(x => x.Id == idLocal);

            if (row == null) return;

            row.Sincronizado = true;
            row.IdServidor = idServidor;
            await _database.UpdateAsync(row);
        }


        public async Task<List<InspeccionDeficiencia>> ListarInspeccionesPendientesAsync()
        {
            await Init();
            return await _database.Table<InspeccionDeficiencia>()
                .Where(x => !x.Sincronizado)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<List<EjecucionReclamoOt>> ListarReclamosOtAsync()
        {
            try
            {
                await Init();
                return await _database!.Table<EjecucionReclamoOt>()
                    .OrderByDescending(x => x.FechaCreacion)
                    .ToListAsync();
            }
            catch
            {
                return new List<EjecucionReclamoOt>();
            }
        }

        public async Task<List<EjecucionReclamoOt>> ListarReclamosOtPendientesAsync()
        {
            try
            {
                await Init();
                return await _database!.Table<EjecucionReclamoOt>()
                    .Where(x => !x.Sincronizado)
                    .OrderByDescending(x => x.FechaCreacion)
                    .ToListAsync();
            }
            catch
            {
                return new List<EjecucionReclamoOt>();
            }
        }

        public async Task<int> EliminarReclamoOtAsync(int ejecucionId)
        {
            try
            {
                await Init();

                var fotos = await _database!.Table<FotoReclamoOt>()
                    .Where(f => f.IdEjecucion == ejecucionId)
                    .ToListAsync();

                foreach (var foto in fotos)
                    await _database.DeleteAsync(foto);

                return await _database.DeleteAsync<EjecucionReclamoOt>(ejecucionId);
            }
            catch
            {
                return 0;
            }
        }

    }
}
