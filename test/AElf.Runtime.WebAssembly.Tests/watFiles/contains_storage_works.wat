(module
	(import "seal0" "seal_return" (func $seal_return (param i32 i32 i32)))
	(import "seal0" "input" (func $seal_input (param i32 i32)))
	(import "seal1" "contains_storage" (func $contains_storage (param i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))

	;; size of input buffer
	;; [0, 4) size of input buffer (128+32 = 160 bytes = 0xA0)
	(data (i32.const 0) "\A0")

	;; [4, 164) input buffer

	(func (export "call")
		;; Receive key
		(call $seal_input
			(i32.const 4)	;; Where we take input and store it
			(i32.const 0)	;; Where we take and store the length of the data
		)
		;; Call seal_clear_storage and save what it returns at 0
		(i32.store (i32.const 0)
			(call $contains_storage
				(i32.const 8)			;; key_ptr
				(i32.load (i32.const 4))	;; key_len
			)
		)
		(call $seal_return
			(i32.const 0)	;; flags
			(i32.const 0)	;; returned value
			(i32.const 4)	;; length of returned value
		)
	)

	(func (export "deploy"))
)