(module
	;; seal_ecdsa_recover(
	;;    signature_ptr: u32,
	;;    message_hash_ptr: u32,
	;;    output_ptr: u32
	;; ) -> u32
	(import "seal0" "ecdsa_recover" (func $seal_ecdsa_recover (param i32 i32 i32) (result i32)))
	(import "env" "memory" (memory 1 1))
	(func (export "call")
		(drop
			(call $seal_ecdsa_recover
				(i32.const 36) ;; Pointer to signature.
				(i32.const 4)  ;; Pointer to message hash.
				(i32.const 36) ;; Pointer for output - public key.
			)
		)
	)
	(func (export "deploy"))

	;; Hash of message.
	(data (i32.const 4)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
	)
	;; Signature
	(data (i32.const 36)
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01\01"
		"\01"
	)
)