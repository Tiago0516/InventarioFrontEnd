using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OrderDashboard.Models;
using ClosedXML.Excel;

namespace OrderDashboard.Controllers
{
    public class InventarioController : Controller
    {
        private readonly HttpClient _httpClient;
        private const int PageSize = 7; // Mostrar 7 elementos por página


        public InventarioController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new System.Uri("http://localhost:5161/api/");
        }

        // GET: Inventario
        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 7)
        {
            List<Inventario> inventario = new List<Inventario>();

            HttpResponseMessage response = await _httpClient.GetAsync("Inventario");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                inventario = JsonConvert.DeserializeObject<List<Inventario>>(data);
            }

            // Filtrar por nombre si se proporciona una búsqueda
            if (!string.IsNullOrEmpty(searchString))
            {
                inventario = inventario
                    .Where(i => i.Nombre.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Paginación
            int totalItems = inventario.Count();
            var paginatedList = inventario.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(paginatedList);
        }

        // GET: Inventario/Details/5
        public async Task<IActionResult> Details(int id)
        {
            Inventario inventario = null;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"Inventario/{id}");

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    inventario = JsonConvert.DeserializeObject<Inventario>(jsonData);
                }
                else
                {
                    ViewData["Error"] = "No se encontró el inventario.";
                }
            }
            catch
            {
                ViewData["Error"] = "Error al conectar con la API.";
            }

            return View(inventario);
        }

        // GET: Inventario/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventario/Create
    [HttpPost]
    public async Task<IActionResult> Create(Inventario model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Obtener lista de medicamentos para verificar duplicados
        HttpResponseMessage getResponse = await _httpClient.GetAsync("Inventario");
        if (getResponse.IsSuccessStatusCode)
        {
            var data = await getResponse.Content.ReadAsStringAsync();
            var inventario = JsonConvert.DeserializeObject<List<Inventario>>(data);

            // Validar si ya existe un medicamento con el mismo nombre (ignorando mayúsculas/minúsculas)
            if (inventario.Any(i => i.Nombre.Equals(model.Nombre, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Nombre", "Ya existe un medicamento con este nombre.");
                return View(model);
            }
        }

        // Si no hay duplicados, proceder con la creación
        var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
        HttpResponseMessage postResponse = await _httpClient.PostAsync("Inventario", jsonContent);

        if (postResponse.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

        // GET: Inventario/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            Inventario inventario = null;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"Inventario/{id}");

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    inventario = JsonConvert.DeserializeObject<Inventario>(jsonData);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                ViewData["Error"] = "Error al conectar con la API.";
            }

            return View(inventario);
        }

        // POST: Inventario/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Inventario inventario)
        {
            if (!ModelState.IsValid) return View(inventario);

            try
            {
                var jsonData = JsonConvert.SerializeObject(inventario);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PutAsync($"Inventario/{id}", content);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));
                else
                    ViewData["Error"] = "No se pudo actualizar el inventario.";
            }
            catch
            {
                ViewData["Error"] = "Error al conectar con la API.";
            }

            return View(inventario);
        }

        // POST: Inventario/Delete/5
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.DeleteAsync($"Inventario/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                ViewData["Error"] = "Error al eliminar el registro.";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DescargarExcel()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("Inventario");
        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("No se pudo obtener la lista de inventario.");
        }

        var data = await response.Content.ReadAsStringAsync();
        var inventario = JsonConvert.DeserializeObject<List<Inventario>>(data);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Inventario");

            // Crear encabezados
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Nombre";
            worksheet.Cell(1, 3).Value = "Descripción";
            worksheet.Cell(1, 4).Value = "Cantidad Stock";
            worksheet.Cell(1, 5).Value = "Precio Unitario";

            // Llenar datos
            int row = 2;
            foreach (var item in inventario)
            {
                worksheet.Cell(row, 1).Value = item.InventarioId;
                worksheet.Cell(row, 2).Value = item.Nombre;
                worksheet.Cell(row, 3).Value = item.Descripcion;
                worksheet.Cell(row, 4).Value = item.Cantidad_Stock;
                worksheet.Cell(row, 5).Value = item.Precio_Unitario;
                row++;
            }

            // Ajustar tamaño de columnas
            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventario.xlsx");
            }
        }
    }


    }
}
