-- =============================================================
--  Billetera Digital — Ejercicio 5: Idempotencia (cabecera Idempotency-Key)
-- =============================================================
-- docker compose lo ejecuta automáticamente SOLO al crear la base por
-- primera vez. Si tu base ya existe, ejecútalo a mano:
--   docker exec -i billetera_postgres psql -U billetera -d billetera_digital < database/04_idempotencia.sql
-- Es idempotente: puede ejecutarse varias veces sin error.

-- Una fila por cada Idempotency-Key recibida en recargas, retiros y transferencias.
-- La clave primaria (cliente_id, clave) es la garantía contra peticiones
-- simultáneas: si dos llegan a la vez con la misma clave, PostgreSQL deja
-- insertar solo a una (INSERT ... ON CONFLICT DO NOTHING).
CREATE TABLE IF NOT EXISTS idempotencia (
    cliente_id      UUID          NOT NULL REFERENCES clientes(id),
    clave           VARCHAR(64)   NOT NULL,
    ruta            VARCHAR(100)  NOT NULL,                 -- /api/transacciones/transferencias
    hash_cuerpo     CHAR(64)      NOT NULL,                 -- SHA-256 de los datos de la petición
    estado          VARCHAR(12)   NOT NULL CHECK (estado IN ('EnProceso','Completada')),
    codigo_http     INTEGER       NULL,
    respuesta       JSONB         NULL,                     -- respuesta que se repite tal cual
    tipo_contenido  VARCHAR(60)   NULL,
    fecha_creacion  TIMESTAMPTZ   NOT NULL DEFAULT now(),
    PRIMARY KEY (cliente_id, clave),
    CONSTRAINT ck_idempotencia_completa CHECK (
        estado = 'EnProceso' OR (codigo_http IS NOT NULL AND respuesta IS NOT NULL)
    )
);

-- La API borra cada hora las claves de más de 24 horas
CREATE INDEX IF NOT EXISTS ix_idempotencia_fecha ON idempotencia (fecha_creacion);
