using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/documentos")]
    [Produces("application/json")]
    public class DocumentosController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _dbConnection;
        private readonly string _uploadsFolder;

        public DocumentosController(IConfiguration configuration, IDbConnection dbConnection)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

            // 📂 Crear la carpeta si no existe
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        /// <summary>
        /// 📂 Subir un documento (Solo PDF)
        /// </summary>
        [HttpPost("subir")]
        public async Task<IActionResult> SubirDocumento([FromForm] IFormFile file, [FromForm] int clienteId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No se proporcionó un archivo válido." });

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                return BadRequest(new { message = "Solo se permiten archivos PDF." });

            var fileName = $"{Guid.NewGuid()}.pdf";
            var filePath = Path.Combine(_uploadsFolder, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var query = "INSERT INTO DocumentosAdmin (ClienteID, NombreDocumento, RutaArchivo, FechaSubida) VALUES (@ClienteID, @NombreDocumento, @RutaArchivo, GETDATE())";
                var parameters = new { ClienteID = clienteId, NombreDocumento = file.FileName, RutaArchivo = filePath };

                await _dbConnection.ExecuteAsync(query, parameters);

                return Ok(new { message = "Documento subido correctamente.", fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al subir el documento.", error = ex.Message });
            }
        }

        /// <summary>
        /// 📑 Obtener la lista de documentos de un cliente
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerDocumentosPorCliente(int clienteId)
        {
            try
            {
                var query = "SELECT ID, NombreDocumento, RutaArchivo, FechaSubida FROM DocumentosAdmin WHERE ClienteID = @ClienteID";
                var documents = await _dbConnection.QueryAsync(query, new { ClienteID = clienteId });
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener documentos.", error = ex.Message });
            }
        }

        /// <summary>
        /// 📥 Descargar un archivo
        /// </summary>
        [HttpGet("descargar/{documentId}")]
        public async Task<IActionResult> DescargarDocumento(int documentId)
        {
            try
            {
                var query = "SELECT RutaArchivo, NombreDocumento FROM DocumentosAdmin WHERE ID = @DocumentID";
                var document = await _dbConnection.QueryFirstOrDefaultAsync(query, new { DocumentID = documentId });

                if (document == null)
                    return NotFound(new { message = "Documento no encontrado." });

                var filePath = document.RutaArchivo.ToString();
                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "El archivo no existe en el servidor." });

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/pdf", document.NombreDocumento.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al descargar el documento.", error = ex.Message });
            }
        }

        /// <summary>
        /// 🗑️ Eliminar un documento
        /// </summary>
        [HttpDelete("eliminar/{documentId}")]
        public async Task<IActionResult> EliminarDocumento(int documentId)
        {
            try
            {
                var query = "SELECT RutaArchivo FROM DocumentosAdmin WHERE ID = @DocumentID";
                var filePath = await _dbConnection.ExecuteScalarAsync<string>(query, new { DocumentID = documentId });

                if (string.IsNullOrEmpty(filePath))
                    return NotFound(new { message = "Documento no encontrado." });

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                var deleteQuery = "DELETE FROM DocumentosAdmin WHERE ID = @DocumentID";
                await _dbConnection.ExecuteAsync(deleteQuery, new { DocumentID = documentId });

                return Ok(new { message = "Documento eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar el documento.", error = ex.Message });
            }
        }
    }
}
