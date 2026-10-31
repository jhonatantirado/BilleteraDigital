-- =============================================================
--  Datos de prueba: 2 clientes con saldo inicial.
--  Contraseña de ambos: Clave123!
-- =============================================================
DO $$
DECLARE
    v_hash TEXT := 'PBKDF2$100000$c2FsZGVwcnVlYmExMjM0NQ==$khxr+wAwoJAjKpl4bTdXBkM20+HYnIbFgNaejebrTg0=';
    c1 UUID := '11111111-1111-1111-1111-111111111111';
    c2 UUID := '22222222-2222-2222-2222-222222222222';
    b1 UUID := 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    b2 UUID := 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
BEGIN
    IF EXISTS (SELECT 1 FROM clientes WHERE id = c1) THEN RETURN; END IF;

    INSERT INTO clientes (id, nombres, apellidos, dni, email, telefono, password_hash)
    VALUES (c1, 'Ana',  'Quispe Rojas', '70123456', 'ana@demo.pe',  '987654321', v_hash),
           (c2, 'Luis', 'Torres Vega',  '70987654', 'luis@demo.pe', '912345678', v_hash);

    INSERT INTO billeteras (id, numero_cuenta, cliente_id, saldo)
    VALUES (b1, '1900000001', c1, 1500.00),
           (b2, '1900000002', c2,  800.00);

    INSERT INTO transacciones (id, billetera_id, tipo, monto, comision, saldo_resultante, descripcion, referencia)
    VALUES (gen_random_uuid(), b1, 'Recarga', 1500.00, 0, 1500.00, 'Saldo inicial', gen_random_uuid()),
           (gen_random_uuid(), b2, 'Recarga',  800.00, 0,  800.00, 'Saldo inicial', gen_random_uuid());
END $$;
