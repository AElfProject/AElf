;; This runs `is_contract` check on zero account address
(module
	(import "seal0" "is_contract" (func $seal_is_contract (param i32) (result i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))

	;; [0, 32) zero-adress
	(data (i32.const 0)
		"\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00"
		"\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00"
	)

	;; [32, 36) here we store the return code of the `seal_is_contract`

	(func (export "deploy"))

	(func (export "call")
		(i32.store
			(i32.const 32)
			(call $seal_is_contract
				(i32.const 0) ;; ptr to destination address
			)
		)
		;; exit with success and take `seal_is_contract` return code to the output buffer
		(call $seal_return (i32.const 0) (i32.const 32) (i32.const 4))
	)
)