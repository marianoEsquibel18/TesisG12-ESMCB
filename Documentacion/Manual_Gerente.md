# Manual del Usuario — Rol: Gerente

**Sistema de Gestión Veterinaria — Veterinaria Ñandubay**  
**Versión:** 2.0  
**Fecha:** Agosto 2026  

---

## 1. Introducción

Bienvenido/a al sistema de gestión de **Veterinaria Ñandubay**. Como **Gerente**, tenés un rol enfocado en la supervisión administrativa, análisis comercial del negocio, y toma de decisiones estratégicas. Este manual te guiará por las secciones y herramientas diseñadas específicamente para tu perfil.

### ¿Qué podés hacer como Gerente?

| Módulo | Funciones |
|--------|-----------|
| **Dashboard** | Ver el estado y resumen general de caja, alertas de stock e indicadores diarios |
| **Mascotas (Pacientes)** | Consultar y buscar fichas de pacientes en modo de solo lectura |
| **Dueños (Clientes)** | Consultar y buscar propietarios en modo de solo lectura |
| **Agenda** | Supervisar y consultar los turnos planificados en modo de solo lectura |
| **Inventario** | Gestión completa (compras, proveedores, depósitos, productos, marcas, categorías, ajustes de stock) |
| **Ventas (POS)** | Consultar el historial de ventas y emitir facturas (sin operar la venta directa) |
| **Reportes** | Generación de informes financieros, operativos y clínicos, y exportación a formato CSV |
| **Veterinarios** | Gestión de alta, baja y modificación de profesionales veterinarios (personal) |
| **Administrar Usuarios** | Consultar la lista de usuarios y examinar el log de auditoría en modo de solo lectura |

> **Nota:** Por motivos de seguridad y confidencialidad médica (principio de acceso mínimo necesario), no tenés acceso a los módulos clínicos de *Historial Clínico* y *Vacunas*, ni a la creación o modificación de usuarios del sistema.

---

## 2. Acceso al Sistema

### 2.1 Iniciar Sesión

1. Abrí el navegador web (Google Chrome o Microsoft Edge).
2. Ingresá la dirección del sistema.
3. Completá tu **usuario** y **contraseña**.
4. Hacé clic en **"Ingresar"**.

### 2.2 Cerrar Sesión

- En la esquina superior derecha, hacé clic en **"Cerrar Sesión"**.

### 2.3 Navegación General

- **Sidebar (Menú lateral):** Contiene los accesos rápidos a los módulos autorizados para tu rol. Se puede colapsar para liberar espacio de pantalla usando el botón en la parte inferior del menú.
- **Topbar (Barra superior):** Muestra el usuario activo, el rol (Gerente) y el acceso para cerrar la sesión.

---

## 3. Dashboard (Centro de Comando)

El dashboard es tu panel de control principal, mostrando un resumen dinámico de la clínica:

- **Caja del Mes:** Total de ingresos acumulados del mes en curso.
- **Turnos de Hoy:** Relación de turnos completados sobre el total planificado del día.
- **Pacientes Activos:** Número de mascotas registradas que están activas.
- **Stock en Alerta:** Cantidad de productos cuya existencia actual está por debajo del stock mínimo.
- **Historial de Caja:** Gráfico interactivo con la evolución de ingresos de los últimos 7 días.
- **Centro de Resoluciones:** Lista de alertas operativas críticas que requieren acciones de reposición de inventario.

---

## 4. Mascotas (Pacientes) — Solo Lectura

Tenés acceso a la información de los pacientes registrados para fines de auditoría y consulta comercial.

### 4.1 Buscar y Consultar
1. Hacé clic en **"Mascotas (Pacientes)"** en el menú lateral.
2. Usá los filtros superiores para buscar por nombre de la mascota, nombre del propietario o especie.
3. Hacé clic sobre el nombre de la mascota para abrir la vista detallada con su información general.

> **Importante:** Tu perfil cuenta con permisos de solo lectura para este módulo. No verás opciones para crear nuevos pacientes ni botones para modificar los datos existentes.

---

## 5. Dueños (Clientes / Propietarios) — Solo Lectura

Podés buscar y consultar las fichas de los propietarios asociados a las mascotas.

### 5.1 Buscar Propietarios
1. En el menú lateral, hacé clic en **"Dueños (Clientes)"**.
2. Podés realizar búsquedas directas ingresando el apellido del propietario o su DNI.

> **Nota:** La creación y edición de propietarios está restringida para tu rol.

---

## 6. Agenda (Supervisión de Turnos) — Solo Lectura

El módulo de Agenda te permite supervisar y controlar la carga horaria y disponibilidad del personal.

### 6.1 Visualización del Calendario
1. Hacé clic en **"Agenda"** en el menú lateral.
2. Navegá por los días del calendario usando las flechas de dirección.
3. Observá el listado de turnos programados, sus horarios, veterinarios asignados y los estados correspondientes (Pendiente, Completado o Cancelado).

> **Nota:** En este rol no es posible crear turnos, cancelarlos o marcarlos como completados.

---

## 7. Inventario (Acceso Completo)

Como Gerente, la gestión de inventario y la relación con los proveedores representa una de tus responsabilidades centrales. Tenés permisos de creación, edición y desactivación lógica.

### 7.1 Gestión de Productos
- **Visualización:** Revisá el stock real, precio de venta, categoría y depósito de cada producto.
- **Alta de Productos:** Completá los datos requeridos (nombre, descripción, categoría, marca, proveedor, depósito, stock inicial y stock mínimo de seguridad).
- **Modificación:** Actualizá la información técnica o comercial de los productos registrados.

### 7.2 Ajustes de Stock
- Utilizá la opción de ajustar stock en la ficha del producto para registrar movimientos manuales de inventario (Entrada, Salida, Ajuste por rotura, Devolución de proveedor), justificando el cambio.

### 7.3 Categorías y Marcas
- Definí y organizá las categorías (ej: Medicamentos, Alimentos, Accesorios) y marcas comerciales para clasificar correctamente los artículos.

### 7.4 Proveedores
- Gestioná la base de datos de los proveedores (Razón Social, CUIT, teléfono, dirección y correo electrónico) para mantener canales de compra fluidos.

### 7.5 Depósitos
- Registrá las diferentes ubicaciones físicas de almacenamiento de mercadería de la clínica.

> **Importante:** Las bajas de productos u otros catálogos se realizan mediante desactivación lógica (soft delete). El registro no se elimina de la base de datos para no afectar la integridad del historial de transacciones.

---

## 8. Ventas (Historial de Transacciones) — Solo Lectura

Tenés visibilidad sobre la facturación de la veterinaria para el control de ingresos.

### 8.1 Consultar Ventas
1. Hacé clic en **"Ventas"** en el menú lateral.
2. Filtrá por rangos de fechas específicos para ver los comprobantes emitidos.
3. Consultá el detalle de cada venta: cliente, método de pago (Efectivo, Tarjeta, Transferencia), listado de productos vendidos, subtotal y total.
4. Generá y consultá las facturas fiscales (A, B o C) asociadas.

> **Nota:** Para realizar ventas directas y operar la caja en el día a día se debe utilizar una cuenta con rol operativo (Recepcionista, Veterinario o Administrador). La pantalla de terminal POS no está habilitada para operaciones de venta en tu perfil.

---

## 9. Reportes y Exportación CSV

Este es el módulo principal para el análisis del negocio. Tenés acceso total para generar estadísticas e informes exportables.

### 9.1 Reportes Disponibles
- **Resumen de Ventas:** Evolución comercial, ingresos totales y transacciones discriminadas por período, vendedor y método de pago.
- **Resumen de Stock:** Valoración total del inventario a precio de costo y venta, y listado detallado de faltantes o productos con stock crítico (bajo mínimo).
- **Rendimiento Operativo (Turnos):** Análisis de productividad de los veterinarios y estadísticas de ausentismo o turnos cancelados.
- **Censo Clínico:** Estadísticas demográficas de las mascotas atendidas (distribución por especie, raza e ingresos).

### 9.2 Exportación de Datos
- En cada sección de reportes encontrarás el botón de exportación. Hacé clic en **"Exportar a CSV"** para descargar las planillas con la información filtrada y poder abrirlas en Microsoft Excel u otras herramientas de hoja de cálculo.

---

## 10. Gestión de Veterinarios (Personal)

La contratación, alta y actualización de la información de los profesionales de la clínica está delegada en tu rol de Gerente.

### 10.1 Alta y Edición de Veterinarios
1. En el menú lateral, hacé clic en **"Veterinarios"**.
2. Hacé clic en **"Nuevo Veterinario"** para registrar un profesional, indicando matrícula, especialidad, datos de contacto y la sucursal asignada.
3. Usá el botón de edición en el listado para actualizar los datos del personal.
4. Podés desactivar (dar de baja lógica) a un profesional si ya no brinda servicios en la clínica.

---

## 11. Visualización de Usuarios y Auditoría — Solo Lectura

Para garantizar la transparencia operativa, podés consultar quiénes acceden al sistema.

### 11.1 Listado de Usuarios
1. Menú lateral → **"Administrar Usuarios"**.
2. Se presentará la lista de todas las cuentas creadas en el sistema, mostrando el nombre completo, el rol asignado, el estado (Activo/Inactivo) y la fecha de su último ingreso.

### 11.2 Log de Auditoría
- Tenés acceso al registro (log) de acciones del sistema. Podés examinar qué usuario realizó qué operación y en qué fecha/hora para resolver discrepancias o supervisar el cumplimiento de tareas.

> **Restricción:** No podés registrar nuevos usuarios, editar sus perfiles, cambiar sus contraseñas o modificar sus roles. Estas acciones son de exclusiva competencia del Administrador del sistema.

---

## 12. Tabla de Roles y Permisos del Sistema

| Funcionalidad | Admin | Gerente | Veterinario | Recepcionista |
|---------------|:-----:|:-------:|:-----------:|:-------------:|
| Dashboard | Sí | Sí | Sí | Sí |
| Mascotas (Pacientes) | Sí | Solo lectura | Sí | Sí |
| Dueños (Clientes) | Sí | Solo lectura | Sí | Sí |
| Agenda | Sí | Solo lectura (supervisión) | Sí | Sí |
| Historial Clínico | Sí | No | Sí | No |
| Vacunas | Sí | No | Sí | No |
| Inventario | Sí | Sí | Sí | Sí |
| Ventas (POS) | Sí | Solo lectura (reportes) | Sí | Sí |
| Reportes | Sí | Sí | Sí | No |
| Veterinarios | Sí | Sí | Sí | Sí |
| Administrar Usuarios | Sí | Solo lectura (logs) | No | No |

---

## 13. Buenas Prácticas de Gestión

### 13.1 Control de Inventario
- Supervisá diariamente las alertas de stock en el Dashboard para programar pedidos a proveedores con suficiente antelación y evitar quiebres de stock.
- Utilizá el reporte de stock valorizado para auditar el capital inmovilizado en las distintas sucursales y depósitos.

### 13.2 Análisis de Rentabilidad
- Exportá el reporte de ventas al finalizar la semana para analizar el desempeño por sucursal y la efectividad de los distintos métodos de pago.
- Evaluá los reportes de rendimiento operativo de los veterinarios para optimizar la asignación de turnos y agendas.

### 13.3 Auditoría Operativa
- Revisá periódicamente los logs de auditoría para verificar que los ajustes manuales de stock y las anulaciones de ventas estén debidamente justificados.

---

## 14. Preguntas Frecuentes

**¿Por qué no puedo registrar una venta si tengo acceso a Ventas?**  
Tu rol tiene permisos de auditoría y análisis sobre las transacciones del negocio, pero no de caja activa. El registro operativo de ventas (POS) está reservado para los roles de recepcionista, veterinario y administrador.

**¿Puedo ver las consultas médicas de una mascota?**  
No. Para preservar la confidencialidad de la historia clínica de los pacientes y cumplir con el principio de acceso mínimo indispensable a datos clínicos, el Historial Clínico y el Plan de Vacunación están restringidos exclusivamente al personal médico (Veterinarios) y al Administrador.

**¿Cómo cambio la contraseña de un recepcionista que la olvidó?**  
Esa acción es competencia exclusiva del Administrador del sistema. Deberás solicitarle al Administrador que realice el blanqueo de la contraseña desde su panel de gestión de usuarios.

**¿Puedo dar de alta una sucursal nueva?**  
No. La creación, edición o desactivación de Sucursales es un proceso de infraestructura del sistema y está reservado exclusivamente para el Administrador.

---

## 15. Glosario

| Término | Significado |
|---------|-------------|
| **Caja del Mes** | Acumulado de ingresos monetarios por ventas facturadas en el mes actual |
| **Soft Delete** | Baja lógica de un registro en la base de datos (marcado como inactivo) para conservar la integridad histórica |
| **Stock de seguridad / mínimo** | Cantidad de unidades de un producto que gatilla una alerta de reposición urgente |
| **Log de Auditoría** | Registro cronológico inalterable de las acciones y operaciones ejecutadas por los usuarios del sistema |
| **CSV** | Formato de archivo de texto plano delimitado por comas, utilizado para importar/exportar datos a planillas de cálculo |
