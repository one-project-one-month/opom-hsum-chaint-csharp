'use client';
import { useState } from 'react';
import { useLanguage } from '@/components/localization';
function parts(value: string) { const [rawHour = '09', minute = '00'] = value.split(':'); const hour24 = Number(rawHour); return { hour: String(hour24 % 12 || 12), minute, period: (hour24 >= 12 ? 'PM' : 'AM') as 'AM' | 'PM' }; }
export function DeliveryTimePicker({ pickupValue = '', dropoffValue = '' }: { pickupValue?: string; dropoffValue?: string }) {
  const { t } = useLanguage();
  const [method, setMethod] = useState<'pickup' | 'dropoff'>(dropoffValue && !pickupValue ? 'dropoff' : 'pickup');
  const initial = parts(method === 'pickup' ? pickupValue : dropoffValue);
  const [hour, setHour] = useState(initial.hour), [minute, setMinute] = useState(initial.minute), [period, setPeriod] = useState<'AM' | 'PM'>(initial.period);
  const chooseMethod = (next: 'pickup' | 'dropoff') => { const nextParts = parts(next === 'pickup' ? pickupValue : dropoffValue); setMethod(next); setHour(nextParts.hour); setMinute(nextParts.minute); setPeriod(nextParts.period); };
  const hour24 = (Number(hour) % 12) + (period === 'PM' ? 12 : 0), value = `${String(hour24).padStart(2, '0')}:${minute}`;
  return <fieldset className="schedule-picker"><legend>{t('အချိန်သတ်မှတ်မည့်ပုံစံ')} *</legend><div className="schedule-options"><label className={method === 'pickup' ? 'active' : ''}><input type="radio" checked={method === 'pickup'} onChange={() => chooseMethod('pickup')} />{t('လာယူရမည်')}</label><label className={method === 'dropoff' ? 'active' : ''}><input type="radio" checked={method === 'dropoff'} onChange={() => chooseMethod('dropoff')} />{t('ပို့ဆောင်ပေးမည်')}</label></div><label>{t('အချိန်')} *</label><input type="hidden" name={method === 'pickup' ? 'pickupTimeOfDay' : 'dropoffTimeOfDay'} value={value} /><div className="friendly-time-picker"><label>{t('နာရီ')}<select value={hour} onChange={event => setHour(event.target.value)}>{Array.from({ length: 12 }, (_, index) => String(index + 1)).map(item => <option key={item}>{item}</option>)}</select></label><span>:</span><label>{t('မိနစ်')}<select value={minute} onChange={event => setMinute(event.target.value)}>{['00', '05', '10', '15', '20', '25', '30', '35', '40', '45', '50', '55'].map(item => <option key={item}>{item}</option>)}</select></label><label>{t('ကာလ')}<select value={period} onChange={event => setPeriod(event.target.value as 'AM' | 'PM')}><option>AM</option><option>PM</option></select></label></div></fieldset>;
}
