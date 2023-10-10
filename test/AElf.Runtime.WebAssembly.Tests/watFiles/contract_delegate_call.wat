(module
	;; seal_delegate_call(
	;;    flags: u32,
	;;    code_hash_ptr: u32,
	;;    input_data_ptr: u32,
	;;    input_data_len: u32,
	;;    output_ptr: u32,
	;;    output_len_ptr: u32
	;;) -> u32
	(import "seal0" "delegate_call" (func $seal_delegate_call (param i32 i32 i32 i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $seal_delegate_call
				(i32.const 0) ;; No flags are set
				(i32.const 4)  ;; Pointer to "callee" code_hash.
				(i32.const 36) ;; Pointer to input data buffer address
				(i32.const 4)  ;; Length of input data buffer
				(i32.const 4294967295) ;; u32 max value is the sentinel value: do not copy output
				(i32.const 0) ;; Length is ignored in this case
			)
		)
	)
	(func (export "deploy"))

	;; Callee code_hash
	(data (i32.const 4)
		"\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11"
		"\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11\11"
	)

	(data (i32.const 36) "\01\02\03\04")
)