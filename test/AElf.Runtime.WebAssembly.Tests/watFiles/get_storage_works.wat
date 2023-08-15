(module
	(import "seal0" "seal_input" (func $seal_input (param i32 i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "seal1" "get_storage" (func $get_storage (param i32 i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))

	;; [0, 4) size of input buffer (160 bytes as we copy the key+len here)
	(data (i32.const 0) "\A0")

	;; [4, 8) size of output buffer
	;; 4k in little endian
	(data (i32.const 4) "\00\10")

	;; [8, 168) input buffer
	;; [168, 4264) output buffer

	(func (export "call")
		;; Receive (key ++ value_to_write)
		(call $seal_input
			(i32.const 8)	;; Pointer to the input buffer
			(i32.const 0)	;; Size of the input buffer
		)
		;; Load a storage value and result of this call into the output buffer
		(i32.store (i32.const 168)
			(call $get_storage
				(i32.const 12)			;; key_ptr
				(i32.load (i32.const 8))	;; key_len
				(i32.const 172)			;; Pointer to the output buffer
				(i32.const 4)			;; Pointer to the size of the buffer
			)
		)
		(call $seal_return
			(i32.const 0)				;; flags
			(i32.const 168)				;; output buffer ptr
			(i32.add				;; length: output size + 4 (retval)
				(i32.load (i32.const 4))
				(i32.const 4)
			)
		)
	)

	(func (export "deploy"))
)