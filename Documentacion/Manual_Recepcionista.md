# Manual del Usuario — Rol: Recepcionista

**Sistema de Gestión Veterinaria — Veterinaria Ñandubay**  
**Versión:** 2.0  
**Fecha:** Mayo 2026  

---

## 1. Introducción

Bienvenido/a al sistema de gestión de **Veterinaria Ñandubay**. Como **Recepcionista**, tu rol es fundamental para la operación diaria de la clínica. Este manual te guiará paso a paso por todas las funciones a las que tenés acceso.

### ¿Qué podés hacer como Recepcionista?

| Módulo | Funciones |
|--------|-----------|
| **Dashboard** | Ver resumen general del día (caja, turnos, pacientes, stock) |
| **Mascotas (Pacientes)** | Registrar, buscar, editar pacientes |
| **Dueños (Clientes)** | Registrar, buscar, editar propietarios |
| **Agenda** | Gestionar turnos (crear, completar, cancelar) |
| **Inventario** | Consultar productos, categorías, marcas, proveedores, depósitos |
| **Ventas (POS)** | Registrar ventas y emitir tickets |
| **Veterinarios** | Consultar el listado de veterinarios |

> **Nota:** No tenés acceso a los módulos de *Historial Clínico*, *Vacunas*, *Reportes* ni *Administrar Usuarios*. Estas secciones están reservadas para veterinarios y administradores.

---

## 2. Acceso al Sistema

### 2.1 Iniciar Sesión

1. Abrí el navegador web (se recomienda **Google Chrome** o **Microsoft Edge**).
2. Ingresá la dirección del sistema (ej: `http://localhost:5173`).
3. En la pantalla de login, ingresá:
   - **Usuario:** tu nombre de usuario asignado.
   - **Contraseña:** tu contraseña.
4. Hacé clic en **"Ingresar"**.

### 2.2 Cerrar Sesión

1. En la esquina superior derecha, hacé clic en **"Cerrar Sesión"**.
2. Serás redirigido a la pantalla de login.

### 2.3 Navegación General

- **Barra lateral (sidebar):** Menú principal con acceso a todos los módulos.
- **Botón de colapsar:** En la parte inferior del sidebar, podés colapsar el menú para ganar espacio (queda solo con iconos).
- **Barra superior (topbar):** Muestra tu nombre de usuario, rol y el botón de cerrar sesión.

---

## 3. Dashboard (Centro de Comando)

Al iniciar sesión, verás el **Centro de Comando** que muestra un resumen del estado actual de la clínica:

| Indicador | Descripción |
|-----------|-------------|
| **Caja del Mes** | Total de ventas acumuladas en el mes actual |
| **Turnos de Hoy** | Cantidad de turnos completados / total del día |
| **Pacientes Activos** | Total de mascotas registradas activas |
| **Stock en Alerta** | Productos con stock por debajo del mínimo |

Además se muestra:
- **Historial de Caja (últimos 7 días):** Gráfico con el total de ventas diarias.
- **Centro de Resoluciones:** Alertas activas que requieren atención (ej: productos con stock bajo).

---

## 4. Mascotas (Pacientes)

### 4.1 Ver listado de pacientes

1. En el menú lateral, hacé clic en **"Mascotas (Pacientes)"**.
2. Se muestra una tabla con todos los pacientes activos.
3. Podés filtrar por:
   - Nombre de la mascota
   - Propietario
   - Especie

### 4.2 Registrar nuevo paciente

1. Hacé clic en el botón **"Nuevo Paciente"**.
2. Completá los campos:
   - **Nombre** (obligatorio): Nombre de la mascota.
   - **Especie** (obligatorio): Seleccionar del listado (Canino, Felino, etc.).
   - **Raza** (opcional): Se filtra automáticamente según la especie.
   - **Sexo** (obligatorio): Macho o Hembra.
   - **Propietario** (obligatorio): Buscar y seleccionar el dueño.
   - **Fecha de Nacimiento** (opcional): Para cálculo automático de edad.
   - **Observaciones** (opcional): Notas adicionales.
3. Hacé clic en **"Guardar"**.

> **Tip:** Si el dueño no está registrado, primero registralo en el módulo de *Dueños (Clientes)*.

### 4.3 Editar paciente

1. En el listado, hacé clic en el ícono de **edición** del paciente deseado.
2. Modificá los campos necesarios.
3. Hacé clic en **"Guardar"**.

### 4.4 Ver detalle del paciente

- Hacé clic sobre el nombre del paciente para ver su información completa: datos, edad calculada, propietario, especie y raza.

---

## 5. Dueños (Clientes / Propietarios)

### 5.1 Ver listado de propietarios

1. En el menú lateral, hacé clic en **"Dueños (Clientes)"**.
2. Se puede buscar por nombre o DNI.

### 5.2 Registrar nuevo propietario

1. Hacé clic en **"Nuevo Propietario"**.
2. Completá los campos:
   - **Nombre** (obligatorio)
   - **Apellido** (obligatorio)
   - **DNI** (obligatorio, debe ser único)
   - **Teléfono** (obligatorio)
   - **Email** (opcional, debe ser un email válido)
   - **Dirección** (opcional)
3. Hacé clic en **"Guardar"**.

> **Importante:** No se pueden registrar dos propietarios con el mismo DNI. Si el sistema muestra un error, verificá que el DNI no esté ya registrado.

### 5.3 Editar propietario

1. Hacé clic en el ícono de **edición**.
2. Se pueden modificar todos los campos excepto el DNI.
3. Hacé clic en **"Guardar"**.

---

## 6. Agenda (Gestión de Turnos)

### 6.1 Ver la agenda

1. En el menú lateral, hacé clic en **"Agenda"**.
2. Se muestra la agenda del día con todos los turnos programados.
3. Podés navegar entre días usando los controles de fecha.

### 6.2 Crear un nuevo turno

1. Hacé clic en **"Nuevo Turno"**.
2. Completá los campos:
   - **Paciente:** Buscar y seleccionar la mascota.
   - **Veterinario:** Seleccionar el profesional.
   - **Servicio:** Tipo de servicio (consulta, vacunación, cirugía, etc.).
   - **Fecha y Hora:** Seleccionar día y horario.
   - **Observaciones** (opcional).
3. Hacé clic en **"Guardar"**.

### 6.3 Gestionar turnos existentes

Cada turno tiene un estado que podés cambiar:

| Estado | Significado | Acciones disponibles |
|--------|-------------|---------------------|
| **Pendiente** | Turno programado | Completar, Cancelar |
| **Completado** | Paciente fue atendido | — (estado final) |
| **Cancelado** | Turno cancelado | — (estado final) |

- **Completar turno:** Al marcar un turno como completado, se genera automáticamente un registro en el historial clínico del paciente.
- **Cancelar turno:** Marca el turno como cancelado.

---

## 7. Inventario

### 7.1 Navegación

En el menú lateral, hacé clic en **"Inventario"** para expandir el submenú:

- **Productos**: Listado completo de productos.
- **Categorías**: Categorías de productos (ej: Medicamentos, Alimentos).
- **Marcas**: Marcas comerciales.
- **Proveedores**: Empresas proveedoras.
- **Depósitos**: Ubicaciones de almacenamiento.

### 7.2 Productos

- Ver todos los productos con su stock actual, precio, categoría y marca.
- Buscar productos por nombre.
- Registrar nuevos productos con: nombre, descripción, precio, stock actual, stock mínimo, categoría, marca, proveedor y depósito.
- Editar productos existentes.

> **Atención al stock:** Los productos con stock por debajo del mínimo aparecerán resaltados y generarán alertas en el Dashboard.

### 7.3 Categorías, Marcas, Proveedores y Depósitos

En cada sección podés:
- Ver el listado completo.
- Registrar nuevos registros.
- Editar registros existentes.
- Eliminar registros (desactivación, no eliminación permanente).

---

## 8. Ventas (POS)

### 8.1 Registrar una venta

1. En el menú lateral, hacé clic en **"Ventas (POS)"**.
2. Seleccioná el propietario/cliente (o dejá como "Consumidor Final").
3. Agregá productos al carrito:
   - Buscá el producto por nombre.
   - Indicá la cantidad.
   - Hacé clic en **"Agregar"**.
4. Revisá el resumen de la venta (productos, cantidades, subtotales, total).
5. Hacé clic en **"Finalizar Venta"**.

> **Nota:** Al finalizar una venta, el stock de cada producto se actualiza automáticamente.

---

## 9. Veterinarios

### 9.1 Consultar veterinarios

1. En el menú lateral, hacé clic en **"Veterinarios"**.
2. Se muestra el listado de veterinarios registrados con su matrícula y especialidad.

> **Nota:** Como recepcionista, solo podés consultar la información de los veterinarios. La creación y edición está reservada para el administrador.

---

## 10. Preguntas Frecuentes

**¿Qué hago si no encuentro a un propietario?**  
Registrá un nuevo propietario en *Dueños (Clientes)* antes de registrar la mascota.

**¿Puedo ver el historial clínico de un paciente?**  
No. El historial clínico es accesible solo para veterinarios y administradores.

**¿Qué pasa si un producto no tiene stock?**  
El sistema no bloqueará la venta, pero el stock quedará en negativo. Avisá al administrador.

**¿Puedo eliminar un turno?**  
No se eliminan turnos, pero podés marcarlos como *Cancelado*.

**¿Puedo recuperar un registro eliminado?**  
Los registros eliminados se desactivan, no se borran. Un administrador puede reactivarlos.

---

## 11. Glosario

| Término | Significado |
|---------|-------------|
| **Paciente** | Mascota registrada en el sistema |
| **Propietario** | Persona responsable de la mascota |
| **POS** | Punto de Venta (Point of Sale) |
| **Stock mínimo** | Cantidad mínima de un producto; si el stock baja de este valor, se genera una alerta |
| **Consumidor Final** | Venta sin asociar a un propietario registrado |
