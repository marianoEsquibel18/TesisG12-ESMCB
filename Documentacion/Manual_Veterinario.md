# Manual del Usuario — Rol: Veterinario

**Sistema de Gestión Veterinaria — Veterinaria Ñandubay**  
**Versión:** 2.0  
**Fecha:** Mayo 2026  

---

## 1. Introducción

Bienvenido/a al sistema de gestión de **Veterinaria Ñandubay**. Como **Veterinario**, tenés acceso a las herramientas clínicas del sistema, incluyendo el historial clínico de los pacientes, el registro de vacunaciones y tratamientos, y la generación de reportes.

### ¿Qué podés hacer como Veterinario?

| Módulo | Funciones |
|--------|-----------|
| **Dashboard** | Ver resumen general del día |
| **Mascotas (Pacientes)** | Consultar, registrar y editar pacientes |
| **Dueños (Clientes)** | Consultar, registrar y editar propietarios |
| **Agenda** | Gestionar turnos (crear, completar, cancelar) |
| **Historial Clínico** | Ficha clínica integral de cada paciente |
| **Vacunas** | Catálogo de vacunas disponibles |
| **Inventario** | Consultar productos, categorías, marcas, proveedores, depósitos |
| **Ventas (POS)** | Registrar ventas |
| **Reportes** | Generar reportes de ventas, stock y tratamientos |
| **Veterinarios** | Consultar listado de profesionales |

---

## 2. Acceso al Sistema

### 2.1 Iniciar Sesión

1. Abrí el navegador web (Google Chrome o Microsoft Edge).
2. Ingresá la dirección del sistema.
3. Completá tu **usuario** y **contraseña**.
4. Hacé clic en **"Ingresar"**.

### 2.2 Cerrar Sesión

- En la esquina superior derecha, hacé clic en **"Cerrar Sesión"**.

### 2.3 Navegación

- **Sidebar:** Menú lateral con acceso a todos los módulos. Podés colapsarlo con el botón inferior para ver solo iconos.
- **Topbar:** Tu nombre, rol y botón de cerrar sesión.

---

## 3. Dashboard (Centro de Comando)

El dashboard muestra un resumen rápido del estado de la clínica:

- **Caja del Mes:** Total de ventas del mes.
- **Turnos de Hoy:** Completados / Total.
- **Pacientes Activos:** Total de mascotas activas.
- **Stock en Alerta:** Productos con stock bajo.
- **Historial de Caja:** Gráfico de ventas de los últimos 7 días.
- **Centro de Resoluciones:** Alertas pendientes (stock bajo, etc.).

---

## 4. Mascotas (Pacientes)

Funciona igual que para el recepcionista. Podés:

- Ver el listado y filtrar pacientes.
- Registrar nuevos pacientes.
- Editar datos de pacientes existentes.
- Ver el detalle de cada paciente.

---

## 5. Dueños (Clientes / Propietarios)

Funciona igual que para el recepcionista. Podés:

- Ver el listado y buscar por nombre o DNI.
- Registrar nuevos propietarios.
- Editar datos de propietarios.

---

## 6. Agenda (Gestión de Turnos)

### 6.1 Ver la agenda

- Hacé clic en **"Agenda"** en el menú lateral.
- Navegá entre días para ver los turnos programados.

### 6.2 Crear y gestionar turnos

- **Crear turno:** Seleccioná paciente, veterinario, servicio, fecha/hora.
- **Completar turno:** Al marcar un turno como *Completado*, se genera automáticamente un registro en **Consultas Médicas** del historial clínico del paciente.
- **Cancelar turno:** Marca el turno como cancelado.

> **Importante:** Al completar un turno, la consulta se registra automáticamente en la ficha clínica del paciente con tu nombre como veterinario, la fecha del turno y el servicio realizado.

---

## 7. Historial Clínico (Ficha Clínica Integral)

Este es tu módulo principal de trabajo clínico.

### 7.1 Acceder al historial

1. En el menú lateral, hacé clic en **"Historial Clínico"**.
2. Se muestra un buscador de pacientes.
3. Buscá al paciente por nombre y hacé clic en **"Ver Ficha"**.

### 7.2 Ficha Clínica del Paciente

La ficha se organiza en **tres pestañas**:

#### 7.2.1 Consultas Médicas

Listado de todas las consultas realizadas al paciente, mostrando:
- **Fecha** de la consulta.
- **Motivo** de la visita.
- **Diagnóstico** realizado.
- **Tratamiento** indicado.
- **Veterinario** que atendió.

**Registrar nueva consulta:**
1. Hacé clic en **"Nueva Consulta"**.
2. Completá: Motivo, Diagnóstico, Tratamiento indicado.
3. Hacé clic en **"Guardar"**.

> Las consultas generadas automáticamente al completar turnos también aparecen aquí.

#### 7.2.2 Plan de Vacunación

Listado de todas las vacunaciones del paciente:
- **Vacuna** aplicada.
- **Fecha** de aplicación.
- **Próxima dosis** programada.
- **Veterinario** que vacunó.

**Registrar nueva vacunación:**
1. Hacé clic en **"Nueva Vacunación"**.
2. Seleccioná la **vacuna** del catálogo.
3. Indicá la **fecha de aplicación** y la **fecha de próxima dosis**.
4. Opcionalmente, agregá **observaciones**.
5. Hacé clic en **"Guardar"**.

> **Requisito:** Debe existir al menos una vacuna en el catálogo (módulo *Vacunas*) para poder registrar una vacunación.

#### 7.2.3 Tratamientos Médicos

Listado de tratamientos del paciente con su estado:

| Estado | Significado |
|--------|-------------|
| **En curso** | Tratamiento activo |
| **Finalizado** | Tratamiento completado |

**Registrar nuevo tratamiento:**
1. Hacé clic en **"Nuevo Tratamiento"**.
2. Completá: Diagnóstico, Descripción, Medicación.
3. Hacé clic en **"Guardar"**.

**Finalizar tratamiento:**
1. En la tabla de tratamientos, encontrá el tratamiento con estado **"En curso"**.
2. En la columna **Acciones**, hacé clic en el botón **"Finalizar"**.
3. El estado cambiará a **"Finalizado"**.

---

## 8. Vacunas (Catálogo)

### 8.1 Ver catálogo de vacunas

1. En el menú lateral, hacé clic en **"Vacunas"**.
2. Se muestra el listado de vacunas disponibles.

### 8.2 Registrar nueva vacuna en el catálogo

1. Hacé clic en **"Nueva Vacuna"**.
2. Completá nombre, descripción y otros campos requeridos.
3. Hacé clic en **"Guardar"**.

> **Nota:** Este catálogo define las vacunas disponibles para aplicar en el módulo de *Plan de Vacunación* del historial clínico.

---

## 9. Inventario

Podés consultar el inventario completo de la clínica:

- **Productos:** Stock actual, precios, categoría, marca.
- **Categorías:** Clasificación de productos.
- **Marcas:** Marcas comerciales registradas.
- **Proveedores:** Empresas proveedoras.
- **Depósitos:** Ubicaciones de almacenamiento.

Podés registrar, editar y gestionar registros en todas las secciones.

---

## 10. Ventas (POS)

Podés registrar ventas de productos:

1. Hacé clic en **"Ventas (POS)"**.
2. Seleccioná el cliente o dejá como "Consumidor Final".
3. Agregá productos al carrito.
4. Finalizá la venta.

---

## 11. Reportes

### 11.1 Acceder a reportes

1. En el menú lateral, hacé clic en **"Reportes"**.
2. Se muestran las secciones de reportes disponibles.

### 11.2 Reportes disponibles

#### Resumen de Ventas
- Resumen de ventas por período.
- Filtros por fecha, vendedor.
- Total de ingresos, cantidad de tickets emitidos.

#### Resumen de Stock
- Estado actual del inventario.
- Productos con stock bajo o agotado.
- Filtros por categoría, marca.

#### Histórico de Tratamientos
- Listado de todos los tratamientos realizados.
- Filtros por paciente, dueño, veterinario y período.
- Muestra: fecha, paciente, dueño, veterinario y estado del tratamiento.

---

## 12. Veterinarios

- Hacé clic en **"Veterinarios"** para ver el listado de profesionales registrados con su matrícula y especialidad.

---

## 13. Preguntas Frecuentes

**¿Cómo registro una consulta desde un turno?**  
Al marcar un turno como *Completado* en la Agenda, la consulta se genera automáticamente en el historial del paciente.

**¿Puedo registrar una vacunación si no hay vacunas en el catálogo?**  
No. Primero debés registrar la vacuna en el módulo *Vacunas*.

**¿Qué pasa si finalizo un tratamiento por error?**  
Actualmente no se puede revertir. Contactá al administrador.

**¿Puedo administrar usuarios?**  
No. La gestión de usuarios está reservada al rol de Administrador.

**¿Puedo ver reportes?**  
Sí. Tenés acceso a los reportes de Ventas, Stock e Histórico de Tratamientos.

---

## 14. Glosario

| Término | Significado |
|---------|-------------|
| **Paciente** | Mascota registrada en el sistema |
| **Propietario** | Persona responsable de la mascota |
| **Ficha Clínica** | Expediente integral de un paciente (consultas, vacunas, tratamientos) |
| **Catálogo de Vacunas** | Listado de vacunas disponibles para aplicación |
| **POS** | Punto de Venta (Point of Sale) |
| **Stock mínimo** | Cantidad mínima de un producto antes de generar alerta |
