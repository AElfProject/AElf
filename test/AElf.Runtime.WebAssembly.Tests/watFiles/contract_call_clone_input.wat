(module
	(import "seal1" "call" (func $seal_call (param i32 i32 i64 i32 i32 i32 i32 i32) (result i32)))
	(import "seal0" "input" (func $seal_input (param i32 i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $seal_call
				(i32.const 11) ;; Set FORWARD_INPUT | CLONE_INPUT | ALLOW_REENTRY bits
				(i32.const 4)  ;; Pointer to "callee" address.
				(i64.const 0)  ;; How much gas to devote for the execution. 0 = all.
				(i32.const 36) ;; Pointer to the buffer with value to transfer
				(i32.const 44) ;; Pointer to input data buffer address
				(i32.const 4)  ;; Length of input data buffer
				(i32.const 4294967295) ;; u32 max value is the sentinel value: do not copy output
				(i32.const 0) ;; Length is ignored in this case
			)
		)

		;; works because the input was cloned
		(call $seal_input (i32.const 0) (i32.const 44))

		;; return the input to caller for inspection
		(call $seal_return (i32.const 0) (i32.const 0) (i32.load (i32.const 44)))
	)

	(func (export "deploy"))

	;; Destination AccountId (ALICE)
	(data (i32.const 4)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)

	;; Amount of value to transfer.
	;; Represented by u64 (8 bytes long) in little endian.
	(data (i32.const 36) "\2A\00\00\00\00\00\00\00")

	;; The input is ignored because we forward our own input
	(data (i32.const 44) "\01\02\03\04")
)