;; This runs `caller_is_root` check on zero account address
(module
	(import "seal0" "caller_is_root" (func $caller_is_root (result i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	;; [0, 4) here the return code of the `caller_is_root` will be stored
	;; we initialize it with non-zero value to be sure that it's being overwritten below
	(data (i32.const 0) "\10\10\10\10")

	(func (export "deploy"))

	(func (export "call")
		(i32.store
			(i32.const 0)
			(call $caller_is_root)
		)
		;; exit with success and take `caller_is_root` return code to the output buffer
		(call $seal_return (i32.const 0) (i32.const 0) (i32.const 4))
	)
)