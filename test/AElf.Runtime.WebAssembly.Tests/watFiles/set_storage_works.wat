(module
	(import "seal0" "input" (func $seal_input (param i32 i32)))
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "seal2" "set_storage" (func $set_storage (param i32 i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))

	;; [0, 4) size of input buffer
	;; 4k in little endian
	(data (i32.const 0) "\00\10")

	;; [4, 4100) input buffer

	(func (export "call")
		;; Receive (key ++ value_to_write)
		(call $seal_input
			(i32.const 4)	;; Pointer to the input buffer
			(i32.const 0)	;; Size of the input buffer
		)
		;; Store the passed value to the passed key and store result to memory
		(i32.store (i32.const 168)
			(call $set_storage
				(i32.const 8)				;; key_ptr
				(i32.load (i32.const 4))		;; key_len
				(i32.add				;; value_ptr = 8 + key_len
					(i32.const 8)
					(i32.load (i32.const 4)))
				(i32.sub				;; value_len (input_size - (key_len + key_len_len))
					(i32.load (i32.const 0))
					(i32.add
						(i32.load (i32.const 4))
						(i32.const 4)
					)
				)
			)
		)
		(call $seal_return
			(i32.const 0)	;; flags
			(i32.const 168)	;; ptr to returned value
			(i32.const 4)	;; length of returned value
		)
	)

	(func (export "deploy"))
)