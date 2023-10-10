(module
	;; seal_transfer(
	;;    account_ptr: u32,
	;;    account_len: u32,
	;;    value_ptr: u32,
	;;    value_len: u32,
	;;) -> u32
	(import "seal0" "transfer" (func $seal_transfer (param i32 i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $seal_transfer
				(i32.const 4)  ;; Pointer to "account" address.
				(i32.const 32)  ;; Length of "account" address.
				(i32.const 36) ;; Pointer to the buffer with value to transfer
				(i32.const 8)  ;; Length of the buffer with value to transfer.
			)
		)
	)
	(func (export "deploy"))

	;; Destination AccountId (ALICE)
	(data (i32.const 4)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)

	;; Amount of value to transfer.
	;; Represented by u64 (8 bytes long) in little endian.
	(data (i32.const 36) "\99\00\00\00\00\00\00\00")
)