import React, { useState } from 'react'

import './listBuilder.scoped.scss'
import { Row, Button, Form, CloseButton } from 'react-bootstrap'

export default function ListBuilder({ name, list, setList, placeholder = '', transform = e => e }) {
  const [entry, setEntry] = useState('')
  const onAdd = () => {
    if (entry.length > 0 && !list.some(item => item.toLowerCase() === entry.toLowerCase())) {
      setList([...list, entry])
      setEntry('')
    }
  }
  return (
    <div className='list-builder'>
      <div className='label'>{name}</div>
      <div className='list'>
        {list.map(e => {
          return (
            <div className='list-entry' key={e}>
              {e}
              <CloseButton onClick={() => setList(list.filter(_e => _e !== e))} variant='white' />
            </div>
          )
        })}
      </div>
      <Row className='submitter'>
        <Form.Control
          value={entry}
          size='sm'
          type='text'
          placeholder={placeholder}
          onChange={e => setEntry(transform(e.target.value))}
          onKeyDown={key => {
            if (key.code === 'Enter') onAdd()
          }}
        />
        <Button size='sm' onClick={onAdd}>
          Add
        </Button>
      </Row>
    </div>
  )
}
